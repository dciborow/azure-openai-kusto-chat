namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    using System;
    using System.Data;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Azure.Core;
    using Azure.Identity;

    using Kusto.Data;
    using Kusto.Data.Common;
    using Kusto.Data.Net.Client;

    using Newtonsoft.Json;

    internal static class KustoHelper
    {
        private static readonly string KustoClusterUri = "https://1es.kusto.windows.net/";
        private static readonly string KustoDatabaseName = "AzureDevOps";

        /// <summary>
        /// Executes the command to show the schema of the AzureDevOps database.
        /// </summary>
        public static async Task<string> ShowDatabaseSchemaAsync()
        {
            string kustoQuery = $".show database {KustoDatabaseName} cslschema";

            // Execute the query and return the schema result as JSON
            return await ExecuteKustoQueryAsync(kustoQuery);
        }

        /// <summary>
        /// Retrieves data by table name, organization name, and build ID.
        /// </summary>
        internal static Task<string> RetrieveDataByOrgAndBuildIdAsync(string tableName, string orgName, string buildId)
            => ExecuteQueryAsync(tableName, orgName, buildId);

        /// <summary>
        /// Retrieves Pull Request data by performing a join between BuildChange and PullRequest tables.
        /// </summary>
        internal static Task<string> RetrievePullRequestsByBuildIdAsync(string orgName, string buildId)
        {
            string additionalQuery = @"
                | join kind=inner PullRequest on $left.BuildChangeId == $right.LastMergeSourceCommitId";
            return ExecuteQueryAsync("BuildChange", orgName, buildId, additionalQuery);
        }

        /// <summary>
        /// General method to build and execute Kusto queries with logging.
        /// </summary>
        private static async Task<string> ExecuteQueryAsync(string tableName, string orgName, string buildId, string additionalQuery = "", object? parameters = null)
        {
            var kustoQuery = $@"
                {tableName}
                | where BuildId == '{buildId}' and OrganizationName == '{orgName}'
                {additionalQuery}".Trim();

            return await ExecuteKustoQueryAsync(kustoQuery, parameters);
        }

        /// <summary>
        /// Executes a Kusto query for a specific cluster and database with pagination.
        /// </summary>
        public static async Task<string> ExecuteKustoQueryForClusterWithoutPaginationAsync(string clusterUri, string databaseName, string query)
        {
            // Log and execute query with pagination
            Logger.LogQuery(query);

            var credentials = new KustoConnectionStringBuilder(clusterUri)
                .WithAadAzureTokenCredentialsAuthentication(CredentialHelper.CreateChainedCredential());

            using (var kustoClient = KustoClientFactory.CreateCslQueryProvider(credentials))
            {
                var clientRequestProperties = new ClientRequestProperties();

                using (var reader = await kustoClient.ExecuteQueryAsync(databaseName, query, clientRequestProperties))
                {
                    var dataTable = new DataTable();
                    dataTable.Load(reader);
                    return JsonConvert.SerializeObject(dataTable);
                }
            }
        }

        /// <summary>
        /// Executes a Kusto query for a specific cluster and database with pagination.
        /// </summary>
        public static async Task<string> ExecuteKustoQueryForClusterWithPaginationAsync(string clusterUri, string databaseName, string query, bool paginated = true, int pageSize = 1000, int pageIndex = 0)
        {
            string paginatedQuery = paginated
                ? $"{query} | skip {pageIndex * pageSize} | take {pageSize}"
                : query;

            // Log and execute query with pagination
            Logger.LogQuery(paginatedQuery);

            var credentials = new KustoConnectionStringBuilder(clusterUri)
                .WithAadAzureTokenCredentialsAuthentication(CredentialHelper.CreateChainedCredential());

            using (var kustoClient = KustoClientFactory.CreateCslQueryProvider(credentials))
            {
                var clientRequestProperties = new ClientRequestProperties();

                using (var reader = await kustoClient.ExecuteQueryAsync(databaseName, paginatedQuery, clientRequestProperties))
                {
                    var dataTable = new DataTable();
                    dataTable.Load(reader);
                    return JsonConvert.SerializeObject(dataTable);
                }
            }
        }

        /// <summary>
        /// Logs and executes a Kusto query, returning the result as a JSON string.
        /// </summary>
        internal static async Task<string> ExecuteKustoQueryAsync(string kustoQuery, object? parameters = null)
        {
            Logger.LogQuery(kustoQuery);

            var credentials = new KustoConnectionStringBuilder(KustoClusterUri)
                .WithAadAzureTokenCredentialsAuthentication(CredentialHelper.CreateChainedCredential());

            using (var kustoClient = KustoClientFactory.CreateCslQueryProvider(credentials))
            {
                var clientRequestProperties = new ClientRequestProperties();
                if (parameters != null)
                {
                    InjectQueryParameters(clientRequestProperties, parameters);
                }

                using (var reader = await kustoClient.ExecuteQueryAsync(KustoDatabaseName, kustoQuery, clientRequestProperties))
                {
                    var dataTable = new DataTable();
                    dataTable.Load(reader);  // Load the data into a DataTable
                    return JsonConvert.SerializeObject(dataTable);  // Convert the DataTable to JSON string
                }
            }
        }

        /// <summary>
        /// Injects query parameters into the ClientRequestProperties for the Kusto query.
        /// </summary>
        private static void InjectQueryParameters(ClientRequestProperties properties, object parameters)
        {
            foreach (var prop in parameters.GetType().GetProperties())
            {
                var value = prop.GetValue(parameters);
                if (value != null)
                {
                    properties.SetParameter(prop.Name, value.ToString());
                }
            }
        }
    }

    internal static class Logger
    {
        /// <summary>
        /// Logs a function call with the given arguments.
        /// </summary>
        public static void LogFunctionCall(string functionName, params string[] args) => LogMessage($"Assistant (function call): {functionName}({string.Join(", ", args)})", ConsoleColor.DarkRed);

        /// <summary>
        /// Logs JSON data with configurable truncation length.
        /// </summary>
        public static void LogJson(string label, object data, int truncateLength = 200)
        {
            string jsonString = System.Text.Json.JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            string output = jsonString.Length > truncateLength ? jsonString.Substring(0, truncateLength) + "..." : jsonString;

            LogMessage($"\t{label}: {output}", ConsoleColor.DarkMagenta);
        }

        /// <summary>
        /// Logs a message with a specific color.
        /// </summary>
        public static void LogMessage(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Logs the query to the console.
        /// </summary>
        internal static void LogQuery(string query)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Assistant (Query): {query}");
            Console.ResetColor();
        }
    }

    internal static class CredentialHelper
    {
        /// <summary>
        /// Creates a ChainedTokenCredential to authenticate with Azure.
        /// </summary>
        internal static TokenCredential CreateChainedCredential()
            => new ChainedTokenCredential(
                new VisualStudioCredential(),
                new VisualStudioCodeCredential(),
                new AzureCliCredential(),
                new DefaultAzureCredential()
            );
    }
}

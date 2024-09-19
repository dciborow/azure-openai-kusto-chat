namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Helpers
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;

    using Kusto.Data;
    using Kusto.Data.Common;
    using Kusto.Data.Net.Client;

    using Newtonsoft.Json;

    internal static class KustoHelper
    {
        /// <summary>
        /// Executes a general Kusto query with optional parameters.
        /// </summary>
        internal static async Task<string> ExecuteKustoQueryAsync(
            string clusterUri,
            string databaseName,
            string kustoQuery,
            object? parameters = null,
            CancellationToken cancellationToken = default)
        {
            LogQuery(kustoQuery);

            var credentials = new KustoConnectionStringBuilder(clusterUri)
                .WithAadAzureTokenCredentialsAuthentication(CredentialHelper.CreateChainedCredential());

            using (var kustoClient = KustoClientFactory.CreateCslQueryProvider(credentials))
            {
                var clientRequestProperties = new ClientRequestProperties
                {
                    ClientRequestId = Guid.NewGuid().ToString(),
                    Application = "DevOpsPlugin"
                };
                if (parameters != null)
                {
                    InjectQueryParameters(clientRequestProperties, parameters);
                }

                using (var reader = await kustoClient.ExecuteQueryAsync(databaseName, kustoQuery, clientRequestProperties, cancellationToken).ConfigureAwait(false))
                {
                    var dataTable = new DataTable();
                    dataTable.Load(reader);
                    return JsonConvert.SerializeObject(dataTable);
                }
            }
        }

        /// <summary>
        /// Retrieves Pull Requests linked to a specific build by performing a join between BuildChange and PullRequest.
        /// </summary>
        internal static async Task<string> RetrievePullRequestsByBuildIdAsync(
            string orgName,
            string buildId,
            string clusterKey,
            CancellationToken cancellationToken = default)
        {
            string clusterUri = GetClusterUri(clusterKey);
            string databaseName = GetDatabaseName(clusterKey);

            string query = $@"
                BuildChange
                | where BuildId == '{EscapeKustoString(buildId)}' and OrganizationName == '{EscapeKustoString(orgName)}'
                | join kind=inner PullRequest on $left.BuildChangeId == $right.LastMergeSourceCommitId
                | project PullRequestId, Title, Status, CreatedDate, UpdatedDate";

            return await ExecuteKustoQueryAsync(clusterUri, databaseName, query, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves data by table name, organization name, and build ID.
        /// </summary>
        internal static async Task<string> RetrieveDataByOrgAndBuildIdAsync(
            string tableName,
            string orgName,
            string buildId,
            string clusterKey,
            CancellationToken cancellationToken = default)
        {
            string clusterUri = GetClusterUri(clusterKey);
            string databaseName = GetDatabaseName(clusterKey);

            string query = $@"
                {EscapeKustoIdentifier(tableName)}
                | where BuildId == '{EscapeKustoString(buildId)}' and OrganizationName == '{EscapeKustoString(orgName)}'";

            return await ExecuteKustoQueryAsync(clusterUri, databaseName, query, null, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Executes an administrative Kusto command such as '.create table' or '.create function'.
        /// </summary>
        /// <param name="clusterUri">The URI of the Kusto cluster.</param>
        /// <param name="databaseName">The name of the Kusto database.</param>
        /// <param name="command">The Kusto administrative command to execute.</param>
        /// <param name="cancellationToken">A cancellation token for managing the asynchronous operation.</param>
        /// <returns>A JSON string indicating success or an error message.</returns>
        internal static async Task<string> ExecuteAdminCommandAsync(
            string clusterUri,
            string databaseName,
            string command,
            CancellationToken cancellationToken = default)
        {
            // Log the admin command being executed
            LogQuery(command);

            var credentials = new KustoConnectionStringBuilder(clusterUri)
                .WithAadAzureTokenCredentialsAuthentication(CredentialHelper.CreateChainedCredential());

            using var kustoClient = KustoClientFactory.CreateCslAdminProvider(credentials);

            await kustoClient.ExecuteControlCommandAsync(databaseName, command);

            return JsonConvert.SerializeObject(new { success = true, message = "Command executed successfully." });
        }

        /// <summary>
        /// Injects query parameters into the ClientRequestProperties for the Kusto query.
        /// </summary>
        private static void InjectQueryParameters(
            ClientRequestProperties properties,
            object parameters)
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

        /// <summary>
        /// Logs the query to the console.
        /// </summary>
        private static void LogQuery(string query)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(@$"## Kusto Query: 
```kql
{query}
```");
            Console.ResetColor();
        }

        /// <summary>
        /// Escapes special characters in Kusto string literals.
        /// </summary>
        private static string EscapeKustoString(string input) => input.Replace("'", "''");

        /// <summary>
        /// Escapes special characters in Kusto identifiers.
        /// </summary>
        private static string EscapeKustoIdentifier(string input) =>
            // Add any necessary escaping for identifiers if needed
            input;

        /// <summary>
        /// Retrieves the cluster URI based on the cluster key.
        /// </summary>
        public static string GetClusterUri(string clusterKey) =>
            // Ideally, retrieve cluster URIs from a configuration source
            clusterKey switch
            {
                "safefly" => "https://safeflycluster.westus.kusto.windows.net/",
                "copilot" => "https://az-copilot-kusto.eastus.kusto.windows.net/",
                "azuredevops" => "https://1es.kusto.windows.net/",
                "appinsights" => "https://ade.applicationinsights.io/subscriptions/ee58f296-1bd7-4fee-bf69-daa9e2d7cec2/resourcegroups/ADOCopilotRG/providers/microsoft.insights/components/CopilotADO",
                "r2d" => "https://az-copilot-kusto.eastus.kusto.windows.net/",
                _ => throw new ArgumentException($"Unknown cluster key: {clusterKey}")
            };

        /// <summary>
        /// Retrieves the database name based on the cluster key.
        /// </summary>
        private static string GetDatabaseName(string clusterKey) =>
            // Ideally, retrieve database names from a configuration source
            LoadKustoClusters()[clusterKey].DatabaseName;


        internal static Dictionary<string, KustoClusterConfig> LoadKustoClusters() => new()
            {
                //{ "safefly", new KustoClusterConfig("safefly", "safefly", "Deployment Requests linked with Build ID") },
                { "copilot", new KustoClusterConfig("copilot", "copilotDevFeedback", "Copilot Risk reports for SafeFly requests based on compared builds of sequential requests") },
                { "azuredevops", new KustoClusterConfig("azuredevops", "AzureDevOps", "Contains Builds, Pull Requests, Commits, and Work Items") },
                { "appinsights", new KustoClusterConfig("appinsights", "CopilotADO", "Contains Log Information for our application") },
                { "r2d", new KustoClusterConfig("r2d", "r2d", "Contains helpful functions saved by Copilot.") },
        };

        /// <summary>
        /// Represents the configuration details of a Kusto cluster.
        /// </summary>
        internal class KustoClusterConfig
        {
            public string Key { get; }
            public string Uri { get; }
            public string DatabaseName { get; }
            public string Description { get; }

            public KustoClusterConfig(string key, string databaseName, string description)
            {
                Key = key;
                Uri = KustoHelper.GetClusterUri(Key);
                DatabaseName = databaseName;
                Description = description;
            }

            public KustoClusterConfig(string key, string uri, string databaseName, string description)
            {
                Key = key;
                Uri = uri;
                DatabaseName = databaseName;
                Description = description;
            }
        }
    }
}

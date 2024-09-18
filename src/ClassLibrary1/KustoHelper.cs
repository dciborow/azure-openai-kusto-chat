namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;

    using Kusto.Data;
    using Kusto.Data.Common;
    using Kusto.Data.Net.Client;

    using Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins;

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
        internal static async Task<string> RetrievePullRequestsByBuildIdAsync(string orgName, string buildId, string clusterKey, CancellationToken cancellationToken = default)
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
        internal static async Task<string> RetrieveDataByOrgAndBuildIdAsync(string tableName, string orgName, string buildId, string clusterKey, CancellationToken cancellationToken = default)
        {
            string clusterUri = GetClusterUri(clusterKey);
            string databaseName = GetDatabaseName(clusterKey);

            string query = $@"
                {EscapeKustoIdentifier(tableName)}
                | where BuildId == '{EscapeKustoString(buildId)}' and OrganizationName == '{EscapeKustoString(orgName)}'";

            return await ExecuteKustoQueryAsync(clusterUri, databaseName, query, null, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Logs the query to the console.
        /// </summary>
        private static void LogQuery(string query)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Kusto Query: {query}");
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
        private static string GetClusterUri(string clusterKey) =>
            // Ideally, retrieve cluster URIs from a configuration source
            clusterKey switch
            {
                "safefly" => "https://safeflycluster.westus.kusto.windows.net/",
                "copilot" => "https://az-copilot-kusto.eastus.kusto.windows.net/",
                "azuredevops" => "https://1es.kusto.windows.net/",
                _ => throw new ArgumentException($"Unknown cluster key: {clusterKey}")
            };

        /// <summary>
        /// Retrieves the database name based on the cluster key.
        /// </summary>
        private static string GetDatabaseName(string clusterKey) =>
            // Ideally, retrieve database names from a configuration source
            clusterKey switch
            {
                "safefly" => "safefly",
                "copilot" => "copilotDevFeedback",
                "azuredevops" => "AzureDevOps",
                _ => throw new ArgumentException($"Unknown cluster key: {clusterKey}")
            };
    }
}

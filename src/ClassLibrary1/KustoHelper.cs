namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    using System;
    using System.Data;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Kusto.Data;
    using Kusto.Data.Common;
    using Kusto.Data.Net.Client;

    using Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins;
    using Microsoft.Azure.Cosmos;
    using Newtonsoft.Json;

    internal static class KustoHelper
    {
        private static readonly string LocalFilePath = "successful_queries.log";
        private static readonly string CosmosDbConnectionString = "your_cosmosdb_connection_string";
        private static readonly string DatabaseName = "your_database_name";
        private static readonly string ContainerName = "your_container_name";

        public static async Task SaveSuccessfulQuery(string query, string result)
        {
            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                Query = query,
                Result = result
            };

            var logJson = JsonConvert.SerializeObject(logEntry);

            // Save to local file
            await File.AppendAllTextAsync(LocalFilePath, logJson + Environment.NewLine);

            // Save to CosmosDB
            using (var cosmosClient = new CosmosClient(CosmosDbConnectionString))
            {
                var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
                var container = await database.Database.CreateContainerIfNotExistsAsync(ContainerName, "/id");

                await container.Container.CreateItemAsync(logEntry, new PartitionKey(logEntry.Timestamp.ToString("yyyy-MM-dd")));
            }
        }

        public static async Task<string> ExecuteKustoQueryForClusterWithoutPaginationAsync(string clusterUri, string databaseName, string query, CancellationToken cancellationToken = default)
        {
            LogQuery(query);

            var credentials = new KustoConnectionStringBuilder(clusterUri)
                .WithAadAzureTokenCredentialsAuthentication(CredentialHelper.CreateChainedCredential());

            using (var kustoClient = KustoClientFactory.CreateCslQueryProvider(credentials))
            {
                var clientRequestProperties = new ClientRequestProperties
                {
                    ClientRequestId = Guid.NewGuid().ToString(),
                    Application = "DevOpsPlugin"
                };
                clientRequestProperties.SetOption("query_timeout", "00:05:00"); // 5 minutes timeout

                using (var reader = await kustoClient.ExecuteQueryAsync(databaseName, query, clientRequestProperties, cancellationToken).ConfigureAwait(false))
                {
                    var dataTable = new DataTable();
                    dataTable.Load(reader);
                    var result = JsonConvert.SerializeObject(dataTable);

                    await SaveSuccessfulQuery(query, result);

                    return result;
                }
            }
        }

        public static async Task<string> ExecuteKustoQueryForClusterWithPaginationAsync(string clusterUri, string databaseName, string query, bool paginated = true, int pageSize = 1000, int pageIndex = 0, CancellationToken cancellationToken = default)
        {
            string paginatedQuery = paginated
                ? $"{query} | skip {pageSize * pageIndex} | take {pageSize}"
                : query;

            LogQuery(paginatedQuery);

            var credentials = new KustoConnectionStringBuilder(clusterUri)
                .WithAadAzureTokenCredentialsAuthentication(CredentialHelper.CreateChainedCredential());

            using (var kustoClient = KustoClientFactory.CreateCslQueryProvider(credentials))
            {
                var clientRequestProperties = new ClientRequestProperties
                {
                    ClientRequestId = Guid.NewGuid().ToString(),
                    Application = "DevOpsPlugin"
                };
                clientRequestProperties.SetOption("query_timeout", "00:05:00"); // 5 minutes timeout

                using (var reader = await kustoClient.ExecuteQueryAsync(databaseName, paginatedQuery, clientRequestProperties, cancellationToken).ConfigureAwait(false))
                {
                    var dataTable = new DataTable();
                    dataTable.Load(reader);
                    var result = JsonConvert.SerializeObject(dataTable);

                    await SaveSuccessfulQuery(paginatedQuery, result);

                    return result;
                }
            }
        }

        internal static async Task<string> ExecuteKustoQueryAsync(string clusterUri, string databaseName, string kustoQuery, object? parameters = null, CancellationToken cancellationToken = default)
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
                    var result = JsonConvert.SerializeObject(dataTable);

                    await SaveSuccessfulQuery(kustoQuery, result);

                    return result;
                }
            }
        }

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

        internal static async Task<string> RetrieveDataByOrgAndBuildIdAsync(string tableName, string orgName, string buildId, string clusterKey, CancellationToken cancellationToken = default)
        {
            string clusterUri = GetClusterUri(clusterKey);
            string databaseName = GetDatabaseName(clusterKey);

            string query = $@"
                {EscapeKustoIdentifier(tableName)}
                | where BuildId == '{EscapeKustoString(buildId)}' and OrganizationName == '{EscapeKustoString(orgName)}'";

            return await ExecuteKustoQueryAsync(clusterUri, databaseName, query, null, cancellationToken).ConfigureAwait(false);
        }

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

        private static void LogQuery(string query)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Kusto Query: {query}");
            Console.ResetColor();
        }

        private static string EscapeKustoString(string input) => input.Replace("'", "''");

        private static string EscapeKustoIdentifier(string input) =>
            input;

        private static string GetClusterUri(string clusterKey) =>
            clusterKey switch
            {
                "safefly" => "https://safeflycluster.westus.kusto.windows.net/",
                "copilot" => "https://az-copilot-kusto.eastus.kusto.windows.net/",
                "azuredevops" => "https://1es.kusto.windows.net/",
                _ => throw new ArgumentException($"Unknown cluster key: {clusterKey}")
            };

        private static string GetDatabaseName(string clusterKey) =>
            clusterKey switch
            {
                "safefly" => "safefly",
                "copilot" => "copilotDevFeedback",
                "azuredevops" => "AzureDevOps",
                _ => throw new ArgumentException($"Unknown cluster key: {clusterKey}")
            };
    }
}

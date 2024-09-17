namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Clients
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;

    using Kusto.Data;
    using Kusto.Data.Common;
    using Kusto.Data.Net.Client;

    using Newtonsoft.Json;

    public static class KustoClient
    {
        /// <summary>
        /// Executes a Kusto query for a specific cluster and database without pagination.
        /// </summary>
        public static async Task<string> ExecuteKustoQueryForClusterWithoutPaginationAsync(string clusterUri, string databaseName, string query, CancellationToken cancellationToken = default)
        {
            KustoHelperFunctions.LogQuery(query);

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
                    return JsonConvert.SerializeObject(dataTable);
                }
            }
        }

        /// <summary>
        /// Executes a Kusto query for a specific cluster and database with optional pagination.
        /// </summary>
        public static async Task<string> ExecuteKustoQueryForClusterWithPaginationAsync(string clusterUri, string databaseName, string query, bool paginated = true, int pageSize = 1000, int pageIndex = 0, CancellationToken cancellationToken = default)
        {
            string paginatedQuery = paginated
                ? $"{query} | skip {pageSize * pageIndex} | take {pageSize}"
                : query;

            KustoHelperFunctions.LogQuery(paginatedQuery);

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
                    return JsonConvert.SerializeObject(dataTable);
                }
            }
        }

        /// <summary>
        /// Executes a general Kusto query with optional parameters.
        /// </summary>
        public static async Task<string> ExecuteKustoQueryAsync(string clusterUri, string databaseName, string kustoQuery, object? parameters = null, CancellationToken cancellationToken = default)
        {
            KustoHelperFunctions.LogQuery(kustoQuery);

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
}

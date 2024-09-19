namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;
    using Microsoft.SemanticKernel;

    public class CosmosDBPlugin : IPlugin
    {
        private CosmosClient _cosmosClient;
        private Database _database;
        private Container _container;

        public async Task InitializeAsync(string endpointUri, string primaryKey, string databaseId, string containerId)
        {
            _cosmosClient = new CosmosClient(endpointUri, primaryKey);
            _database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            _container = await _database.CreateContainerIfNotExistsAsync(containerId, "/id");
        }

        [KernelFunction("query_cosmos_db")]
        [Description("Queries Cosmos DB for items based on a SQL query.")]
        [return: Description("Returns the query results as a JSON string.")]
        public async Task<string> QueryCosmosDBAsync(
            [Description("The SQL query to execute.")] string sqlQuery)
        {
            var queryDefinition = new QueryDefinition(sqlQuery);
            var queryResultSetIterator = _container.GetItemQueryIterator<dynamic>(queryDefinition);

            var results = new List<dynamic>();
            while (queryResultSetIterator.HasMoreResults)
            {
                var response = await queryResultSetIterator.ReadNextAsync();
                results.AddRange(response);
            }

            return JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        }

        public string Help()
        {
            return @"CosmosDBPlugin provides functionalities to initialize and query Cosmos DB.
Available functions:
1. InitializeAsync(endpointUri, primaryKey, databaseId, containerId) - Initializes the Cosmos DB client.
2. QueryCosmosDBAsync(sqlQuery) - Executes a SQL query against the Cosmos DB and returns the results as a JSON string.";
        }
    }
}

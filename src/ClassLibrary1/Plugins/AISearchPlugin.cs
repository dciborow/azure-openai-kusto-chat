namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.Connectors.AzureAISearch;

    public class AISearchPlugin : IPlugin
    {
        private AzureAISearchClient _aiSearchClient;

        public async Task InitializeAsync(string endpoint, string apiKey, string indexName)
        {
            _aiSearchClient = new AzureAISearchClient(endpoint, apiKey, indexName);
        }

        [KernelFunction("query_ai_search")]
        [Description("Queries AI Search for items based on a search query.")]
        [return: Description("Returns the search results as a JSON string.")]
        public async Task<string> QueryAISearchAsync(
            [Description("The search query to execute.")] string searchQuery)
        {
            var results = await _aiSearchClient.SearchAsync(searchQuery);
            return JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        }

        public string Help()
        {
            return @"AISearchPlugin provides functionalities to initialize and query AI Search.
Available functions:
1. InitializeAsync(endpoint, apiKey, indexName) - Initializes the AI Search client.
2. QueryAISearchAsync(searchQuery) - Executes a search query against the AI Search and returns the results as a JSON string.";
        }
    }
}

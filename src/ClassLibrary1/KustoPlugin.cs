namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text.Json;

    using global::Microsoft.SemanticKernel;

    public class KustoPlugin
    {
        private readonly Dictionary<string, KustoClusterConfig> _clusters;

        // Constructor that initializes clusters with unique keys
        public KustoPlugin()
        {
            _clusters = new Dictionary<string, KustoClusterConfig>
            {
                { "safefly", new KustoClusterConfig("safefly", "https://safeflycluster.westus.kusto.windows.net/", "safefly", "Deployment Requests linked with Build ID") },
                { "copilot", new KustoClusterConfig("copilot", "https://az-copilot-kusto.eastus.kusto.windows.net/", "copilotDevFeedback", "Copilot Risk reports for SafeFly requests based on compared builds of sequential requests") },
                { "azuredevops", new KustoClusterConfig("azuredevops", "https://1es.kusto.windows.net/", "AzureDevOps", "Contains Builds, Pull Requests, Commits, and Work Items") }
            };
        }

        /// <summary>
        /// Lists the available Kusto clusters and their databases with descriptions.
        /// </summary>
        [KernelFunction("list_kusto_databases")]
        [Description("Lists the available Kusto clusters and their databases.")]
        [return: Description("Returns the list of clusters and databases.")]
        public string ListKustoDatabases()
        {
            Logger.LogFunctionCall("KustoPlugin.ListKustoDatabases");

            var databases = new Dictionary<string, string>();

            foreach (var cluster in _clusters)
            {
                databases[$"{cluster.Value.Uri} database: {cluster.Value.DatabaseName}"] = $"{cluster.Value.Description}";
            }

            var result = JsonSerializer.Serialize(databases, new JsonSerializerOptions { WriteIndented = true });
            Logger.LogJson("Tool", result);
            return result;
        }

        /// <summary>
        /// Executes a Kusto query for a specified cluster, database, and table with pagination support.
        /// </summary>
        [KernelFunction("query_kusto_database")]
        [Description("Executes a query against the specified Kusto database with pagination support. Adjust pageSize and pageIndex for larger data sets.")]
        [return: Description("Returns the result of the query as a JSON string.")]
        public async Task<string> QueryKustoDatabaseAsync(
            [Description("Key of the Kusto cluster to query (e.g., 'azuredevops', 'safefly', etc.).")] string clusterKey,
            [Description("The Kusto query string to execute.")] string query,
            [Description("Boolean flag indicating whether to paginate the results. Default is false.")] bool paginated = false,
            [Description("The number of rows to return in a single page when paginated. Default is 1000 rows per page.")] int pageSize = 1000,
            [Description("The index of the page to retrieve when paginated. Starts at 0 for the first page.")] int pageIndex = 0)
        {
            if (!_clusters.TryGetValue(clusterKey, out var cluster))
            {
                return $"Cluster with key '{clusterKey}' not found.";
            }

            // Execute paginated query using the KustoHelper
            var result = await KustoHelper.ExecuteKustoQueryForClusterAsync(cluster.Uri, cluster.DatabaseName, query, paginated, pageSize, pageIndex);

            // Handle large responses by checking token limits
            if (result.Length > 1048576)  // Example: 1 MB limit
            {
                return HandleLargeResponse(result);
            }

            return result ?? "No results found.";
        }

        /// <summary>
        /// Handles responses that exceed the token limit by breaking them into chunks and asking the user how to proceed.
        /// </summary>
        private string HandleLargeResponse(string response)
        {
            const int chunkSize = 1048576 / 2;  // Example chunk size for safe token limits
            var responseChunks = new List<string>();

            for (int i = 0; i < response.Length; i += chunkSize)
            {
                responseChunks.Add(response.Substring(i, Math.Min(chunkSize, response.Length - i)));
            }

            // Ask the user for input on how to handle large data
            Console.WriteLine("The response is too large. How would you like to proceed?");
            Console.WriteLine("1. Return the first chunk");
            Console.WriteLine("2. Return all chunks in sequence");
            Console.WriteLine("3. Discard the response");

            var userInput = Console.ReadLine();
            switch (userInput)
            {
                case "1":
                    return responseChunks[0];
                case "2":
                    return string.Join("\n---\n", responseChunks);
                case "3":
                    return "Response discarded.";
                default:
                    return "Invalid option selected.";
            }
        }


        /// <summary>
        /// Provides a list of tables in the specified Kusto database.
        /// </summary>
        [KernelFunction("list_kusto_tables")]
        [Description("Lists all the tables in the specified Kusto database.")]
        [return: Description("Returns the list of tables in the specified database as a JSON string.")]
        public async Task<string> ListKustoTablesAsync(string clusterKey)
        {
            Logger.LogFunctionCall("KustoPlugin.ListKustoTablesAsync", clusterKey);

            // Find the cluster by key
            if (!_clusters.TryGetValue(clusterKey, out var cluster))
            {
                Logger.LogMessage($"Cluster with key '{clusterKey}' not found.", ConsoleColor.Red);
                return $"Cluster with key '{clusterKey}' not found.";
            }

            string query = $".show database {clusterKey} cslschema";
            var result = await KustoHelper.ExecuteKustoQueryForClusterAsync(cluster.Uri, cluster.DatabaseName, query);
            Logger.LogJson("Tool", result);

            return result ?? "No tables found.";
        }

        // Helper method to get cluster by key
        private KustoClusterConfig? GetClusterByKey(string clusterKey)
        {
            Logger.LogFunctionCall("KustoPlugin.GetClusterByKey", clusterKey);

            if (_clusters.TryGetValue(clusterKey, out var cluster))
            {
                return cluster;
            }

            Logger.LogMessage($"Cluster with key '{clusterKey}' not found.", ConsoleColor.Red);
            return null;
        }
    }

    // Kusto Cluster Config class to store cluster details
    public class KustoClusterConfig
    {
        public string Name { get; }
        public string Uri { get; }
        public string DatabaseName { get; }
        public string Description { get; }

        public KustoClusterConfig(string name, string uri, string databaseName, string description)
        {
            Name = name;
            Uri = uri;
            DatabaseName = databaseName;
            Description = description;
        }
    }
}

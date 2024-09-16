namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text.Json;
    using System.Threading.Tasks;
    using global::Microsoft.SemanticKernel;

    public class ClearwaterPlugin
    {
        /// <summary>
        /// Provides help information for the Clearwater plugin.
        /// </summary>
        [KernelFunction("help")]
        [Description("Provides help information for the Clearwater plugin.")]
        [return: Description("Returns the help information as a string.")]
        public string Help()
        {
            return "Clearwater Plugin Help: This plugin provides various functions to interact with the 1es Kusto cluster database's AzureDevOps.";
        }
    }

    public class DevOpsPlugin
    {
        /// <summary>
        /// Retrieves build information by organization name and build ID.
        /// </summary>
        [KernelFunction("get_build_info")]
        [Description("Retrieves build information by organization name and build ID.")]
        [return: Description("Returns the build information as a JSON string or a message if no build is found.")]
        public async Task<string> GetBuildInfoAsync(
            [Description("The name of the organization (e.g., 'msazure').")] string orgName,
            [Description("The build ID to look up.")] string buildId)
        {
            var result = await ExecuteAndLogQueryAsync("Build", orgName, buildId, "No build found");
            await KustoHelper.SaveSuccessfulQuery($"Build info query for Org: {orgName}, BuildId: {buildId}", result);
            return result;
        }

        /// <summary>
        /// Retrieves work items linked to a specific build.
        /// </summary>
        [KernelFunction("get_workitem_by_org_build")]
        [Description("Retrieves work items linked to a specific build.")]
        [return: Description("Returns the work items associated with the build in JSON format or a message if no work items are found.")]
        public async Task<string> GetWorkItemsByBuildAsync(
            [Description("The name of the organization (e.g., 'msazure').")] string orgName,
            [Description("The build ID to look up.")] string buildId)
        {
            var result = await ExecuteAndLogQueryAsync("BuildWorkItem", orgName, buildId, "No work items found");
            await KustoHelper.SaveSuccessfulQuery($"Work items query for Org: {orgName}, BuildId: {buildId}", result);
            return result;
        }

        /// <summary>
        /// Retrieves commits linked to a specific build.
        /// </summary>
        [KernelFunction("get_commits_by_org_build")]
        [Description("Retrieves commits linked to a specific build.")]
        [return: Description("Returns the commits associated with the build in JSON format or a message if no commits are found.")]
        public async Task<string> GetCommitsByBuildAsync(
            [Description("The name of the organization (e.g., 'msazure').")] string orgName,
            [Description("The build ID to look up.")] string buildId)
        {
            var result = await ExecuteAndLogQueryAsync("BuildChange", orgName, buildId, "No commits found");
            await KustoHelper.SaveSuccessfulQuery($"Commits query for Org: {orgName}, BuildId: {buildId}", result);
            return result;
        }

        /// <summary>
        /// Retrieves Pull Requests linked to a specific build by performing a join between BuildChange and PullRequest.
        /// </summary>
        [KernelFunction("get_pull_requests_by_build")]
        [Description("Retrieves Pull Requests linked to a specific build by performing a join between BuildChange and PullRequest.")]
        [return: Description("Returns the pull requests associated with the build in JSON format or a message if no pull requests are found.")]
        public async Task<string> GetPullRequestsByBuildAsync(
            [Description("The name of the organization (e.g., 'msazure').")] string orgName,
            [Description("The build ID to look up.")] string buildId)
        {
            Logger.LogFunctionCall("DevOpsPlugin.GetPullRequestsByBuildAsync", orgName, buildId);

            var pullRequests = await KustoHelper.RetrievePullRequestsByBuildIdAsync(orgName, buildId);
            Logger.LogJson("Tool", pullRequests);

            await KustoHelper.SaveSuccessfulQuery($"Pull requests query for Org: {orgName}, BuildId: {buildId}", pullRequests);

            return string.IsNullOrEmpty(pullRequests) ? $"No pull requests found for Org: {orgName} and BuildId: {buildId}" : pullRequests;
        }

        // Central method to handle execution and logging for simple queries
        private async Task<string> ExecuteAndLogQueryAsync(string tableName, string orgName, string buildId, string noDataMessage)
        {
            Logger.LogFunctionCall($"DevOpsPlugin.Get{tableName}ByOrgAndBuildAsync", orgName, buildId);

            var result = await KustoHelper.RetrieveDataByOrgAndBuildIdAsync(tableName, orgName, buildId);
            Logger.LogJson("Tool", result);

            return string.IsNullOrEmpty(result) ? $"{noDataMessage} for Org: {orgName} and BuildId: {buildId}" : result;
        }
    }

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
            var result = await KustoHelper.ExecuteKustoQueryForClusterWithPaginationAsync(cluster.Uri, cluster.DatabaseName, query, paginated, pageSize, pageIndex);

            // Handle large responses by checking token limits
            if (result.Length > 1048576)  // Example: 1 MB limit
            {
                return HandleLargeResponse(result);
            }

            await KustoHelper.SaveSuccessfulQuery($"Kusto query for Cluster: {clusterKey}, Query: {query}", result);

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
            var result = await KustoHelper.ExecuteKustoQueryForClusterWithoutPaginationAsync(cluster.Uri, cluster.DatabaseName, query);
            Logger.LogJson("Tool", result);

            await KustoHelper.SaveSuccessfulQuery($"List tables query for Cluster: {clusterKey}", result);

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

    public class LargeRequestHelper
    {
        private readonly int MaxTokenSize = 100000;  // Maximum token size per GPT request
        private readonly int MaxRowsPerRequest = 500;  // Maximum rows to send in one request
        private string _fullOutput = string.Empty;  // Buffer to store the final output

        /// <summary>
        /// Processes a large data set in batches and interacts with GPT to determine how much more can be processed.
        /// </summary>
        /// <param name="query">The Kusto query to execute.</param>
        /// <param name="clusterUri">The URI of the Kusto cluster.</param>
        /// <param name="databaseName">The Kusto database name.</param>
        /// <returns>Returns the final processed output as a string.</returns>
        public async Task<string> ProcessLargeDataSetAsync(string query, string clusterUri, string databaseName)
        {
            int pageSize = MaxRowsPerRequest;
            int pageIndex = 0;
            bool continueProcessing = true;

            while (continueProcessing)
            {
                // Fetch the next batch of data
                string batchResult = await KustoHelper.ExecuteKustoQueryForClusterWithPaginationAsync(clusterUri, databaseName, query, true, pageSize, pageIndex);

                if (string.IsNullOrEmpty(batchResult))
                    break;  // No more data, end the loop

                // Append to the full output
                _fullOutput += batchResult;

                // Check if GPT can handle more data
                bool canProcessMore = AskGPTIfMoreDataCanBeProcessed(_fullOutput);

                if (!canProcessMore)
                {
                    // Ask user if they want to continue processing more chunks
                    continueProcessing = AskUserIfTheyWantToContinue();
                }
                else
                {
                    // Increase page index to fetch the next batch
                    pageIndex++;
                }
            }

            return _fullOutput;
        }

        /// <summary>
        /// Asks GPT whether it can handle more rows based on the current data size.
        /// </summary>
        private bool AskGPTIfMoreDataCanBeProcessed(string currentOutput) =>
            // Here, send the currentOutput to GPT and ask if it can handle more data.
            // For simplicity, let's simulate GPT's decision-making:
            currentOutput.Length < MaxTokenSize;

        /// <summary>
        /// Asks the user if they want to continue processing more chunks.
        /// </summary>
        private bool AskUserIfTheyWantToContinue()
        {
            // Notify the user about the current batch, chunks processed, and ask if they want to continue
            Console.WriteLine($"Processed {_fullOutput.Length} tokens so far. Do you want to process more? (y/n)");
            string input = Console.ReadLine();
            return input?.ToLower() == "y";
        }
    }
}

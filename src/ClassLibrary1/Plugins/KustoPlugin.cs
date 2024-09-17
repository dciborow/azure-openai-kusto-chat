namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.SemanticKernel;

    public class KustoPlugin
    {
        private readonly Dictionary<string, KustoClusterConfig> _clusters;
        private static readonly string[] value = new[]
                    {
                        "Update the query with important columns by adding a '| project' section.",
                        "Use 'kusto_query_count' to determine the number of results in your previous query.",
                        "If the issue continues, add a 'take' statement or decrease the number of rows taken if it was already included.",
                    };

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
            LogFunctionCall("KustoPluginVNext.ListKustoDatabases");

            var databases = new Dictionary<string, string>();

            foreach (var cluster in _clusters)
            {
                databases[$"{cluster.Key} database: {cluster.Value.DatabaseName}"] = $"{cluster.Value.Description}";
            }

            var result = JsonSerializer.Serialize(databases, new JsonSerializerOptions { WriteIndented = true });
            LogJson("Tool", result);
            return result;
        }

        /// <summary>
        /// Executes a Kusto query to count the number of rows returned by the original query.
        /// </summary>
        [KernelFunction("kusto_query_count")]
        [Description("Appends | count to a query to determine how many rows are being returned to help query large datasets.")]
        [return: Description("Returns the result of the query as a JSON string.")]
        public async Task<string> KustoQueryCountAsync(
            [Description("Key of the Kusto cluster to query (e.g., 'azuredevops', 'safefly', etc.).")] string clusterKey,
            [Description("The Kusto query string to execute.")] string query,
            CancellationToken cancellationToken = default)
        {
            if (!ValidateClusterKey(clusterKey, out var cluster, out var errorResponse))
            {
                return errorResponse;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return CreateErrorResponse("The 'query' parameter cannot be null or empty.");
            }

            string modifiedQuery = $"{query} | count";

            try
            {
                var result = await KustoHelper
                    .ExecuteKustoQueryForClusterWithPaginationAsync(cluster!.Uri, cluster.DatabaseName, modifiedQuery, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return result ?? CreateErrorResponse("No results found.");
            }
            catch (Exception ex)
            {
                LogError(ex, "An error occurred while executing the Kusto count query.");
                return CreateErrorResponse("An internal error occurred while executing the count query.");
            }
        }

        /// <summary>
        /// Executes a Kusto query against the specified database with optional pagination.
        /// </summary>
        [KernelFunction("kusto_query")]
        [Description("Executes a query against the specified Kusto database with pagination support. Adjust pageSize and pageIndex for larger data sets.")]
        [return: Description("Returns the result of the query as a JSON string.")]
        public async Task<string> QueryKustoDatabaseAsync(
            [Description("Key of the Kusto cluster to query (e.g., 'azuredevops', 'safefly', etc.).")] string clusterKey,
            [Description("The Kusto query string to execute.")] string query,
            [Description("Boolean flag indicating whether to paginate the results. Default is false.")] bool paginated = false,
            [Description("The number of rows to return in a single page when paginated. Default is 1000 rows per page.")] int pageSize = 1000,
            [Description("The index of the page to retrieve when paginated. Starts at 0 for the first page.")] int pageIndex = 0,
            CancellationToken cancellationToken = default)
        {
            if (!ValidateClusterKey(clusterKey, out var cluster, out var errorResponse))
            {
                return errorResponse!;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return CreateErrorResponse("The 'query' parameter cannot be null or empty.");
            }

            if (pageSize <= 0)
            {
                return CreateErrorResponse("The 'pageSize' must be a positive integer.");
            }

            if (pageIndex < 0)
            {
                return CreateErrorResponse("The 'pageIndex' cannot be negative.");
            }

            try
            {
                var result = await KustoHelper
                    .ExecuteKustoQueryForClusterWithPaginationAsync(cluster!.Uri, cluster.DatabaseName, query, paginated, pageSize, pageIndex, cancellationToken)
                    .ConfigureAwait(false);

                return ProcessQueryResult(result);
            }
            catch (Exception ex)
            {
                LogError(ex, "An error occurred while executing the Kusto query.");
                return CreateErrorResponse("An internal error occurred while executing the query.");
            }
        }

        /// <summary>
        /// Provides a list of tables in the specified Kusto database.
        /// </summary>
        [KernelFunction("list_kusto_tables")]
        [Description("Lists all the tables in the specified Kusto database.")]
        [return: Description("Returns the list of tables in the specified database as a JSON string.")]
        public async Task<string> ListKustoTablesAsync(
            [Description("Key of the Kusto cluster to query (e.g., 'azuredevops', 'safefly', etc.).")] string clusterKey,
            CancellationToken cancellationToken = default)
        {
            LogFunctionCall("KustoPluginVNext.ListKustoTablesAsync", clusterKey);

            if (!ValidateClusterKey(clusterKey, out var cluster, out var errorResponse))
            {
                LogMessage($"Cluster with key '{clusterKey}' not found.", ConsoleColor.Red);
                return errorResponse;
            }

            string query = $".show database {cluster.DatabaseName} cslschema";

            try
            {
                var result = await KustoHelper
                    .ExecuteKustoQueryForClusterWithoutPaginationAsync(cluster.Uri, cluster.DatabaseName, query, cancellationToken)
                    .ConfigureAwait(false);
                LogJson("Tool", result);

                return result ?? CreateErrorResponse("No tables found.");
            }
            catch (Exception ex)
            {
                LogError(ex, "An error occurred while retrieving the database schema.");
                return CreateErrorResponse("An internal error occurred while retrieving the database schema.");
            }
        }

        /// <summary>
        /// Retrieves Pull Requests linked to a specific build by performing a join between BuildChange and PullRequest.
        /// </summary>
        [KernelFunction("get_pull_requests_by_build")]
        [Description("Retrieves Pull Requests linked to a specific build by performing a join between BuildChange and PullRequest.")]
        [return: Description("Returns the pull requests associated with the build in JSON format or a message if no pull requests are found.")]
        public async Task<string> GetPullRequestsByBuildAsync(
            [Description("Key of the Kusto cluster to query (e.g., 'azuredevops', 'safefly', etc.).")] string clusterKey,
            [Description("The name of the organization (e.g., 'msazure').")] string orgName,
            [Description("The build ID to look up.")] string buildId,
            CancellationToken cancellationToken = default)
        {
            LogFunctionCall("KustoPluginVNext.GetPullRequestsByBuildAsync", orgName, buildId);

            var pullRequests = await KustoHelper.RetrievePullRequestsByBuildIdAsync(orgName, buildId, clusterKey, cancellationToken).ConfigureAwait(false);
            LogJson("Tool", pullRequests);

            return string.IsNullOrEmpty(pullRequests) ? $"No pull requests found for Org: {orgName} and BuildId: {buildId}" : pullRequests;
        }

        /// <summary>
        /// Processes the query result and checks if it exceeds a token limit, providing
        /// suggestions for modifying the query if necessary.
        /// </summary>
        private static string ProcessQueryResult(string result)
        {
            const int tokenLimit = 120000;
            if (result.Length / 4 > tokenLimit)
            {
                Logger.LogMessage($"Response is too large. Considering how we should proceed. Result length: {result.Length}", ConsoleColor.DarkGreen);
                return JsonSerializer.Serialize(new
                {
                    message = $"The response length was {result.Length} characters and exceeded the token limit.",
                    suggestions = value
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            return result ?? "No results found.";
        }

        /// <summary>
        /// Validates the cluster key and retrieves the corresponding cluster configuration.
        /// </summary>
        /// <param name="clusterKey">The key of the cluster.</param>
        /// <param name="cluster">The retrieved cluster configuration.</param>
        /// <param name="errorResponse">The error response if validation fails.</param>
        /// <returns>True if the cluster key is valid; otherwise, false.</returns>
        private bool ValidateClusterKey(string clusterKey, out KustoClusterConfig? cluster, out string? errorResponse)
        {
            if (string.IsNullOrWhiteSpace(clusterKey))
            {
                cluster = null;
                errorResponse = CreateErrorResponse("The 'clusterKey' parameter cannot be null or empty.");
                return false;
            }

            if (!_clusters.TryGetValue(clusterKey, out cluster))
            {
                errorResponse = CreateErrorResponse($"Cluster with key '{clusterKey}' not found.");
                return false;
            }

            errorResponse = null;
            return true;
        }

        /// <summary>
        /// Creates a standardized error response in JSON format.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>A JSON string representing the error response.</returns>
        private static string CreateErrorResponse(string message)
            => JsonSerializer.Serialize(
                new { error = true, message },
                new JsonSerializerOptions { WriteIndented = true });

        /// <summary>
        /// Logs a function call with the given arguments.
        /// </summary>
        /// <param name="functionName">Name of the function being called.</param>
        /// <param name="args">Arguments passed to the function.</param>
        private void LogFunctionCall(string functionName, params string[] args)
            => Logger.LogFunctionCall($"KustoPluginVNext.{functionName}", args);

        /// <summary>
        /// Logs JSON data with a specified label.
        /// </summary>
        /// <param name="label">Label for the JSON data.</param>
        /// <param name="data">The data to log.</param>
        private void LogJson(string label, string data)
            => Logger.LogJson(label, data);

        /// <summary>
        /// Logs an error with exception details.
        /// </summary>
        /// <param name="ex">The exception that was thrown.</param>
        /// <param name="context">Contextual information about where the error occurred.</param>
        private void LogError(Exception ex, string context)
            => Logger.LogError($"Error in KustoPluginVNext: {context} - {ex.Message}");

        /// <summary>
        /// Logs a message with a specific color.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The color to use for the message.</param>
        private void LogMessage(string message, ConsoleColor color)
            => Logger.LogMessage(message, color);
    }
}

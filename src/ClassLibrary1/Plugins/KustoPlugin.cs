namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.AzureCore.ReadyToDeploy.Vira.Helpers;
    using Microsoft.SemanticKernel;

    using static Microsoft.AzureCore.ReadyToDeploy.Vira.Helpers.KustoHelper;

    public class KustoPlugin : PluginBase
    {
        private const string KustoFunctionsLogPath = "kusto_functions.log";

        private readonly Dictionary<string, KustoClusterConfig> _clusters;
        private static readonly string[] tokenLimitExceededSuggestions = new[]
                    {
                        "try again after choosing important columns from the schema and select them using ' | project'.",
                        "Use 'kusto_query_count' to determine the number of results in your previous query.",
                        "If the issue continues, add a 'take' statement or decrease the number of rows taken if it was already included.",
                    };

        // Constructor that initializes clusters with unique keys
        public KustoPlugin()
        {
            _clusters = new()
            {
                { "safefly", new KustoClusterConfig("safefly", "https://safeflycluster.westus.kusto.windows.net/", "safefly", "Deployment Requests linked with Build ID") },
                { "copilot", new KustoClusterConfig("copilot", "https://az-copilot-kusto.eastus.kusto.windows.net/", "copilotDevFeedback", "Copilot Risk reports for SafeFly requests based on compared builds of sequential requests") },
                { "azuredevops", new KustoClusterConfig("azuredevops", "https://1es.kusto.windows.net/", "AzureDevOps", "Contains Builds, Pull Requests, Commits, and Work Items") },
                { "appinsights", new KustoClusterConfig("appinsights",  "https://ade.applicationinsights.io/subscriptions/ee58f296-1bd7-4fee-bf69-daa9e2d7cec2/resourcegroups/ADOCopilotRG/providers/microsoft.insights/components/CopilotADO", "CopilotADO", "Contains Log Information for our application") },
                { "r2d", new KustoClusterConfig("r2d", "https://az-copilot-kusto.eastus.kusto.windows.net/", "r2d", "The R2D Kusto Cluster may be used to save functions for later use.") },
            };
        }

        [KernelFunction("kusto_query_best_practices")]
        [Description("Should always run this step before using kusto_query. Retrieve a set of Kusto Best Practices that can be used before running a kusto query.")]
        [return: Description("Returns the list of best practices to follow when writing kusto queries.")]
        public string KustoBestPractices()
        {
            LogFunctionCall("KustoPluginVNext.KustoBestPractices");

            return @"The following are best practices for GPT to use Kusto
1. When getting errors always check that you have the correct columns for the table.
    Semantic error: 'summarize' operator: Failed to resolve scalar expression named
    Semantic error: 'project' operator: Failed to resolve scalar expression named
2. If you have trouble finding a specific value, check the distinct values of the column
3. you may not cast values during joins
";
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
            LogJson("### Tool", result);
            return result;
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
                    .ExecuteKustoQueryAsync(cluster.Uri, cluster.DatabaseName, query, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                LogJson("### Tool", result);

                return result ?? CreateErrorResponse("No tables found.");
            }
            catch (Exception ex)
            {
                LogError(ex, "An error occurred while retrieving the database schema.");
                return CreateErrorResponse("An internal error occurred while retrieving the database schema.");
            }
        }

        /// <summary>
        /// Executes a Kusto query against the specified database with optional pagination.
        /// </summary>
        [KernelFunction("kusto_query")]
        [Description("Executes a query against the specified Kusto database.")]
        [return: Description("Returns the result of the query as a JSON string.")]
        public async Task<string> QueryKustoAsync(
            [Description("Key of the Kusto cluster to query (e.g., 'azuredevops', 'safefly', etc.).")] string clusterKey,
            [Description("The Kusto query string to execute.")] string query,
            CancellationToken cancellationToken = default)
        {
            LogFunctionCall("KustoPluginVNext.QueryKustoAsync", clusterKey, query.Substring(0, 10) + "...");

            if (!ValidateClusterKey(clusterKey, out var cluster, out var errorResponse))
            {
                return errorResponse!;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return CreateErrorResponse("The 'query' parameter cannot be null or empty.");
            }

            try
            {
                var result = await KustoHelper
                    .ExecuteKustoQueryAsync(cluster!.Uri, cluster.DatabaseName, query, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return ProcessQueryResult(result);
            }
            catch (Exception ex)
            {
                LogError(ex, "An error occurred while executing the Kusto query.");
                return CreateErrorResponse("An internal error occurred while executing the query.");
            }
        }

        [KernelFunction("list_kusto_functions")]
        [Description("Lists kusto functions that have been previously saved by Copilot. Check these to help simplify kusto queries.")]
        /// <summary>
        /// Retrieves a list of functions in the specified Kusto cluster.
        /// </summary>
        /// <param name="clusterKey">The key of the Kusto cluster to query.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A JSON-formatted string containing the list of functions.</returns>
        public async Task<string> ListKustoFunctionsAsync(
            string clusterKey,
            [Description("An optional filter for function names. If provided, only functions whose names contain this filter will be returned. Injected as | where Name contains '{functionNameFilter}'")] string? functionNameFilter = null,
            CancellationToken cancellationToken = default)
        {
            LogFunctionCall("KustoPluginVNext.ListKustoFunctionsAsync", clusterKey);

            if (!ValidateClusterKey(clusterKey, out var cluster, out var errorResponse))
            {
                LogMessage($"Cluster with key '{clusterKey}' not found.", ConsoleColor.Red);
                return errorResponse!;
            }

            string query = ".show functions";

            if (!string.IsNullOrWhiteSpace(functionNameFilter))
            {
                query += $" | where Name contains '{functionNameFilter}'";
            }

            try
            {
                var result = await KustoHelper
                    .ExecuteKustoQueryAsync(
                        cluster!.Uri,
                        cluster.DatabaseName,
                        query,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                LogJson("### Tool", result);

                return result ?? CreateErrorResponse("No functions found.");
            }
            catch (Exception ex)
            {
                LogError(ex, "An error occurred while retrieving the list of functions.");
                return CreateErrorResponse("An internal error occurred while retrieving the list of functions.");
            }
        }

        /// <summary>
        /// Creates a Kusto function in the specified database based on the complete function definition provided.
        /// </summary>
        /// <param name="clusterKey">The key of the Kusto cluster where the function will be created.</param>
        /// <param name="functionDefinition">The entire Kusto command for creating the function, including .create function syntax.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A JSON-formatted string indicating success or an error message.</returns>
        [KernelFunction("create_kusto_function")]
        [Description("Creates a Kusto function in the specified database based on the complete function definition provided.")]

        public async Task<string> CreateKustoFunctionAsync(
            [Description("The entire Kusto command for creating the function, including .create function syntax.")] string functionDefinition,
            CancellationToken cancellationToken = default)
        {
            LogFunctionCall($"KustoPluginVNext.CreateKustoFunction({functionDefinition})");
            var clusterKey = "r2d";

            if (!ValidateClusterKey(clusterKey, out var cluster, out var errorResponse))
            {
                return errorResponse!;
            }

            if (string.IsNullOrWhiteSpace(functionDefinition) ||
                !functionDefinition.Trim().StartsWith(".create function", StringComparison.OrdinalIgnoreCase) || !functionDefinition.Trim().StartsWith(".alter function", StringComparison.OrdinalIgnoreCase))
            {
                return CreateErrorResponse("Invalid function definition. Ensure it begins with '.create function'.");
            }

            try
            {
                var result = await KustoHelper
                    .ExecuteAdminCommandAsync(
                        cluster!.Uri,
                        cluster.DatabaseName,
                        functionDefinition,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return ProcessQueryResult(result);
            }
            catch (Exception ex)
            {
                LogError(ex, "An error occurred while creating the Kusto function.");
                return CreateErrorResponse("An internal error occurred while creating the Kusto function.");
            }
        }
        /// <summary>
        /// Creates a table in the specified Kusto database with the provided command.
        /// We will only allow admin commands to our 'r2d' database.
        /// </summary>
        /// [KernelFunction("create_kusto_function")]
        [Description("Creates a Kusto table in the r2d database.")]
        [return: Description("Returns a JSON string indicating success or an error message.")]

        public async Task<string> CreateTableAsync(
            string createTableCommand,
            CancellationToken cancellationToken = default)
        {
            LogFunctionCall("KustoPluginVNext.CreateTableAsync");
            var clusterKey = "r2d";

            if (string.IsNullOrWhiteSpace(createTableCommand) ||
                !createTableCommand.Trim().StartsWith(".create table", StringComparison.OrdinalIgnoreCase))
            {
                return CreateErrorResponse("Invalid create table command. Ensure it begins with '.create table'.");
            }

            string clusterUri = GetClusterUri(clusterKey);
            string databaseName = clusterKey;

            return await ExecuteAdminCommandAsync(clusterUri, databaseName, createTableCommand, cancellationToken);
        }
        /// <summary>
        /// Processes the query result and checks if it exceeds a token limit, providing
        /// suggestions for modifying the query if necessary.
        /// </summary>
        private static string ProcessQueryResult(string result)
        {
            if (TryTokenLimit(result))
            {
                LoggerHelper.LogMessage($"Response is too large. Considering how we should proceed. Result length: {result.Length}", ConsoleColor.DarkGreen);
                return string.Join("\n", tokenLimitExceededSuggestions);
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
        private bool ValidateClusterKey(
            string clusterKey,
            out KustoClusterConfig? cluster,
            out string? errorResponse)
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
        private static new string CreateErrorResponse(
            string message)
            => JsonSerializer.Serialize(
                new { error = true, message },
                new JsonSerializerOptions { WriteIndented = true });

        /// <summary>
        /// Logs a function call with the given arguments.
        /// </summary>
        /// <param name="functionName">Name of the function being called.</param>
        /// <param name="args">Arguments passed to the function.</param>
        private protected new void LogFunctionCall(
            string functionName,
            params string[] args)
            => LoggerHelper.LogFunctionCall($"KustoPluginVNext.{functionName}", args);

        /// <summary>
        /// Logs JSON data with a specified label.
        /// </summary>
        /// <param name="label">Label for the JSON data.</param>
        /// <param name="data">The data to log.</param>
        private protected new void LogJson(string label, string data)
            => LoggerHelper.LogJson(label, data);

        /// <summary>
        /// Logs an error with exception details.
        /// </summary>
        /// <param name="ex">The exception that was thrown.</param>
        /// <param name="context">Contextual information about where the error occurred.</param>
        private void LogError(Exception ex, string context)
            => LoggerHelper.LogError($"Error in KustoPluginVNext: {context} - {ex.Message}");

        /// <summary>
        /// Logs a message with a specific color.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The color to use for the message.</param>
        private void LogMessage(string message, ConsoleColor color)
            => LoggerHelper.LogMessage(message, color);

        /// <summary>
        /// Tries to determine if the result exceeds the token limit.
        /// 
        /// The estimation formula for token limit is: length / 4
        /// We use 5 as a multiplier to account for the additional characters in the JSON response.
        /// 
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <returns>Determinant if the result exceeds the token limit.</returns>
        private static bool TryTokenLimit(string result)
        {
            const int tokenLimit = 120000;

            return result.Length / 5 > tokenLimit;
        }

        public override string Help() => throw new NotImplementedException();

        [Serializable]
        private class TokenLimitException : Exception
        {
            public TokenLimitException()
            {
            }

            public TokenLimitException(string? message) : base(message)
            {
            }

            public TokenLimitException(string? message, Exception? innerException) : base(message, innerException)
            {
            }
        }
    }
}

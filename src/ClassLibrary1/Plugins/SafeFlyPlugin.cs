namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Kusto.Data;
    using Kusto.Data.Net.Client;

    using Microsoft.SemanticKernel;

    /// <summary>
    /// Represents a plugin for interacting with SafeFly, such as retrieving the latest SafeFly requests.
    /// </summary>
    public class SafeFlyPlugin : PluginBase
    {
        private readonly string _logsDirectory;
        private readonly string _safeFlyRequestsFilePath;

        // Kusto connection details
        private readonly string _kustoClusterUri;
        private readonly string _kustoDatabase;
        private readonly string _kustoAppId;
        private readonly string _kustoAppKey;
        private readonly string _kustoTenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeFlyPlugin"/> class.
        /// </summary>
        /// <param name="logsDirectory">The directory where log files will be stored. Defaults to 'Logs' directory in the application root if not provided.</param>
        /// <param name="kustoClusterUri">The URI of the Kusto (Azure Data Explorer) cluster.</param>
        /// <param name="kustoDatabase">The name of the Kusto database to query.</param>
        /// <param name="kustoAppId">The application ID for Azure AD authentication.</param>
        /// <param name="kustoAppKey">The application key for Azure AD authentication.</param>
        /// <param name="kustoTenantId">The tenant ID for Azure AD authentication.</param>
        public SafeFlyPlugin(
            string? logsDirectory = null,
            string kustoClusterUri = "",
            string kustoDatabase = "",
            string kustoAppId = "",
            string kustoAppKey = "",
            string kustoTenantId = "")
        {
            // Initialize logging directories and files
            _logsDirectory = logsDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            try
            {
                // Ensure the logs directory exists
                Directory.CreateDirectory(_logsDirectory);
            }
            catch (Exception ex)
            {
                LogError("SafeFlyPlugin.Constructor", $"Failed to create logs directory '{_logsDirectory}': {ex.Message}");
                throw new DirectoryNotFoundException($"Unable to create or access the logs directory at '{_logsDirectory}'.", ex);
            }

            // Define file paths
            _safeFlyRequestsFilePath = Path.Combine(_logsDirectory, "safefly_requests.log");

            // Initialize Kusto connection details
            _kustoClusterUri = kustoClusterUri;
            _kustoDatabase = kustoDatabase;
            _kustoAppId = kustoAppId;
            _kustoAppKey = kustoAppKey;
            _kustoTenantId = kustoTenantId;
        }

        /// <summary>
        /// Executes the primary function of the SafeFlyPlugin.
        /// Designed to handle various actions based on parameters.
        /// </summary>
        /// <param name="parameters">Parameters required for execution: action (get_latest_SafeFly_requests), [count], [filter].</param>
        /// <returns>A JSON string indicating the result of the operation.</returns>
        public override async Task<string> ExecuteAsync(params string[] parameters)
        {
            LogFunctionCall("SafeFlyPlugin.ExecuteAsync", parameters);

            if (parameters.Length < 1)
            {
                return CreateErrorResponse("Insufficient parameters. Required: action. Optional: count, filter.");
            }

            string action = parameters[0].ToLower();

            return action switch
            {
                "get_latest_safefly_requests" => await GetLatestSafeFlyRequestsAsync(parameters.Length > 1 ? parameters[1] : "100", parameters.Length > 2 ? parameters[2] : null),
                _ => CreateErrorResponse($"Unknown action '{action}'. Valid actions are: get_latest_SafeFly_requests.")
            };
        }

        /// <summary>
        /// Retrieves the latest SafeFly requests based on specified parameters.
        /// </summary>
        /// <param name="count">Optional. The number of SafeFly requests to retrieve. Defaults to 100.</param>
        /// <param name="filter">Optional. A filter criterion (e.g., Status) to narrow down the SafeFly requests.</param>
        /// <returns>A JSON string containing the latest SafeFly requests or an error message.</returns>
        [KernelFunction("get_latest_SafeFly_requests")]
        [Description("Retrieves the latest SafeFly requests based on specified parameters.")]
        [return: Description("Returns a JSON string containing the latest SafeFly requests or an error message.")]
        public async Task<string> GetLatestSafeFlyRequestsAsync(string count, string? filter)
        {
            LogFunctionCall("SafeFlyPlugin.GetLatestSafeFlyRequestsAsync", new string[] { count, filter ?? "" });

            if (!int.TryParse(count, out int requestCount) || requestCount <= 0)
            {
                return CreateErrorResponse("Invalid 'count' parameter. It must be a positive integer.");
            }

            try
            {
                // Construct the Kusto query
                string query = $"SafeFlyRequests | sort by Timestamp desc | take {requestCount}";

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    // Example: Filter by Status
                    query += $" | where Status == '{filter}'";
                }

                LogInfo("SafeFlyPlugin.GetLatestSafeFlyRequestsAsync", $"Executing Kusto query: {query}");

                // Execute the Kusto query
                var kustoConnectionStringBuilder = new KustoConnectionStringBuilder(_kustoClusterUri, _kustoDatabase)
                    .WithAadAzureTokenCredentialsAuthentication(CredentialHelper.CreateChainedCredential());

                using var queryProvider = KustoClientFactory.CreateCslQueryProvider(kustoConnectionStringBuilder);
                using var reader = queryProvider.ExecuteQuery(query);

                // Read the results
                var results = new List<SafeFlyRequest>();
                while (reader.Read())
                {
                    var request = new SafeFlyRequest
                    {
                        PartitionKey = reader.GetString(reader.GetOrdinal("PartitionKey")),
                        Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp")),
                        ServiceName = reader.GetString(reader.GetOrdinal("ServiceName")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                        SubmittedBy = reader.GetString(reader.GetOrdinal("SubmittedBy")),
                        RollOutInfo = reader.IsDBNull(reader.GetOrdinal("RollOutInfo")) ? null : reader.GetString(reader.GetOrdinal("RollOutInfo")),
                        ScopeAndImpact = reader.IsDBNull(reader.GetOrdinal("ScopeAndImpact")) ? null : reader.GetString(reader.GetOrdinal("ScopeAndImpact"))
                    };
                    results.Add(request);
                }

                // Serialize the results to JSON
                var jsonResult = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });

                // Log the successful retrieval
                LogJson("SafeFlyPlugin.GetLatestSafeFlyRequestsAsync", jsonResult);
                LogInfo("SafeFlyPlugin.GetLatestSafeFlyRequestsAsync", $"Retrieved {results.Count} SafeFly requests successfully.");

                // Save the results to a log file
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Type = "SafeFlyRequests",
                    Content = jsonResult
                };

                await AppendLogAsync(_safeFlyRequestsFilePath, logEntry);

                return jsonResult;
            }
            catch (Exception ex)
            {
                string error = $"An error occurred while retrieving SafeFly requests: {ex.Message}";
                LogError("SafeFlyPlugin.GetLatestSafeFlyRequestsAsync", error);
                return CreateErrorResponse(error);
            }
        }

        /// <summary>
        /// Provides help information for the SafeFlyPlugin.
        /// </summary>
        /// <returns>A JSON string detailing the available SafeFlyPlugin functions.</returns>
        [KernelFunction("safeflyplugin_help")]
        [Description("Provides detailed information about the available SafeFlyPlugin functions, including descriptions, parameters, and return types.")]
        [return: Description("Returns a JSON string detailing the available SafeFlyPlugin functions.")]
        public override string Help()
        {
            LogFunctionCall("SafeFlyPlugin.Help");

            var helpInfo = new List<object>
            {
                new
                {
                    name = "get_latest_SafeFly_requests",
                    description = "Retrieves the latest SafeFly requests based on specified parameters.",
                    parameters = new List<object>
                    {
                        new
                        {
                            name = "count",
                            type = "Integer",
                            description = "Optional. The number of SafeFly requests to retrieve. Defaults to 100."
                        },
                        new
                        {
                            name = "filter",
                            type = "String",
                            description = "Optional. A filter criterion (e.g., Status) to narrow down the SafeFly requests."
                        }
                    },
                    returns = "Returns a JSON string containing the latest SafeFly requests or an error message."
                }
            };

            var result = JsonSerializer.Serialize(new { functions = helpInfo }, new JsonSerializerOptions { WriteIndented = true });
            LogJson("SafeFlyPlugin.Help", result);
            LogInfo("SafeFlyPlugin.Help", "Help information provided successfully.");
            return result;
        }

        /// <summary>
        /// Appends a log entry to the specified file in JSON format.
        /// </summary>
        /// <param name="filePath">The path of the file to append the log entry to.</param>
        /// <param name="logEntry">The log entry to append.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task AppendLogAsync(string filePath, LogEntry logEntry)
        {
            try
            {
                var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });
                var logLine = $"{json}{Environment.NewLine}";
                await File.AppendAllTextAsync(filePath, logLine);
                LogJson("SafeFlyPlugin.AppendLogAsync", $"Appended log to '{filePath}'.");
            }
            catch (Exception ex)
            {
                LogError("SafeFlyPlugin.AppendLogAsync", $"Failed to append log to '{filePath}': {ex.Message}");
                throw; // Re-throw to allow higher-level handling
            }
        }

        /// <summary>
        /// Represents a structured log entry.
        /// </summary>
        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Type { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }

        /// <summary>
        /// Represents a SafeFly request.
        /// </summary>
        private class SafeFlyRequest
        {
            public string PartitionKey { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string ServiceName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string SubmittedBy { get; set; } = string.Empty;
            public string? RollOutInfo { get; set; }
            public string? ScopeAndImpact { get; set; }
        }
    }
}

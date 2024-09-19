namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System.ComponentModel;

    using Microsoft.SemanticKernel;

    /// <summary>
    /// Represents a plugin for interacting with SafeFly, such as retrieving the latest SafeFly requests.
    /// </summary>
    public class SafeFlyPlugin : KustoPlugin
    {
        [KernelFunction("safefly_query_best_practices")]
        [Description("Should always use this step before using lookup_safefly_requests_kusto. This contains helpful information that should be considered before querying SafeFly with Kusto.")]
        [return: Description("Returns the list of best practices to follow when writing kusto queries for SafeFly.")]
        public static string SafeFlyBestPractices()
        {
            LogFunctionCall("SafeFlyPlugin.SafeFlyBestPractices");

            return @"If you have any issues, consider the following best practices querying SafeFly.
1. When getting project errors, could check that you have the correct columns.
2. If you have trouble finding a specific value, check the distinct values of the column.
3. The most common table is SafeFly Requests, the Id, ServiceName, and LastsStatusUpdateDate are useful fields.
4. The service names may or may not start with 'Azure', use lookup_safefly_services for a full list of service names.
5. When querying for SafeFly requests, always remove duplicates 'SafeFlyRequest | summarize arg_max(LastStatusUpdateDate, *) by Id'

" + KustoBestPractices();
        }

        /// <summary>
        /// Retrieves the Safefly requests from Kusto.
        /// Users can filter by service name and provide optional custom queries.
        /// </summary>
        [KernelFunction("lookup_safefly_requests_kusto")]
        [Description("Retrieves the Safefly requests from Kusto. Users can provide a service name for filtering results. But, first use safefly_query_best_practices to remember the quarks of querying SafeFly.")]
        [return: Description("Returns the result of the query as a JSON formatted string containing the Safefly requests.")]
        public async Task<string> QueryKustoSafeflyRequestsAsync(
            [Description("The Kusto query string to execute, e.g., 'SafeFlyRequest | count' or similar.")] string query,
            [Description("Optional: Service name to filter results. If not provided, retrieves all requests.")] string? serviceName = null,
            CancellationToken cancellationToken = default)
        {
            LogFunctionCall("KustoPluginVNext.QuerySafeflyRequestsKustoAsync", query.Substring(0, 40) + "...");

            // Check to include summarization only if the input query doesn't already contain it and starts with a valid reference
            if (query.StartsWith("SafeFlyRequest", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Replace("SafeFlyRequest |", "SafeFlyRequest | summarize arg_max(LastStatusUpdateDate, *) by Id |");
            }
            else
            {
                query = $"SafeFlyRequest | summarize arg_max(LastStatusUpdateDate, *) by Id | {query}";
            }

            // If a service name is provided, include the where clause in the query
            if (!string.IsNullOrWhiteSpace(serviceName))
            {
                query += $" | where ServiceName contains \"{serviceName}\"";
            }

            // Execute the query
            return await QueryKustoAsync("safefly", query, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Retrieves the Safefly requests from Kusto.
        /// Users can filter by service name and provide optional custom queries.
        /// </summary>
        [KernelFunction("lookup_safefly_services")]
        [Description("Retrieves the list of Safefly Service teams from Kusto which can be used to help lookup services for users.")]
        [return: Description("Returns the result of the query as a JSON formatted string containing the Safefly requests.")]
        public async Task<string> GetSafeFlyServices(
            [Description("Optional fitlers that will be appended to the kusto query SafeFlyRequest | distinct ServiceName += filters")] string? filters = null,
            CancellationToken cancellationToken = default)
        {
            LogFunctionCall("KustoPluginVNext.GetSafeFlyServices");

            var query = "SafeFlyRequest | distinct ServiceName"
                + filters ?? string.Empty;

            return await QueryKustoAsync("safefly", query, cancellationToken: cancellationToken);
        }
    }
}

namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.ComponentModel;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.AzureCore.ReadyToDeploy.Vira.Helpers;
    using Microsoft.SemanticKernel;

    /// <summary>
    /// Provides methods to retrieve build-related information from DevOps, 
    /// such as builds, work items, commits, and pull requests associated 
    /// with a specific organization and build ID.
    /// </summary>
    public class DevOpsPlugin : KustoPlugin
    {
        private const string DefaultClusterKey = "azuredevops"; // Assuming DevOpsPlugin uses the 'azuredevops' cluster by default
        /// <summary>
        /// Retrieves Pull Requests linked to a specific build by performing a join between BuildChange and PullRequest.
        /// </summary>
        [KernelFunction("get_pull_requests_by_build")]
        [Description("Retrieves Pull Requests linked to a specific build by performing a join between BuildChange and PullRequest.")]
        [return: Description("Returns the pull requests associated with the build in JSON format or a message if no pull requests are found.")]
        public static async Task<string> GetPullRequestsByBuildAsync(
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
        /// Retrieves build information by organization name and build ID.
        /// </summary>
        [KernelFunction("get_build_info")]
        [Description("Retrieves build information by organization name and build ID.")]
        [return: Description("Returns the build information as a JSON string or a message if no build is found.")]
        public static async Task<string> GetBuildInfoAsync(
            [Description("The name of the organization (e.g., 'msazure').")] string orgName,
            [Description("The build ID to look up.")] string buildId,
            CancellationToken cancellationToken = default)
            => await ExecuteAndLogQueryAsync("Build", orgName, buildId, "No build found", cancellationToken);

        /// <summary>
        /// Retrieves work items linked to a specific build.
        /// </summary>
        [KernelFunction("get_workitem_by_org_build")]
        [Description("Retrieves work items linked to a specific build.")]
        [return: Description("Returns the work items associated with the build in JSON format or a message if no work items are found.")]
        public static async Task<string> GetWorkItemsByBuildAsync(
            [Description("The name of the organization (e.g., 'msazure').")] string orgName,
            [Description("The build ID to look up.")] string buildId,
            CancellationToken cancellationToken = default)
            => await ExecuteAndLogQueryAsync("BuildWorkItem", orgName, buildId, "No work items found", cancellationToken);

        /// <summary>
        /// Retrieves commits linked to a specific build.
        /// </summary>
        [KernelFunction("get_commits_by_org_build")]
        [Description("Retrieves commits linked to a specific build.")]
        [return: Description("Returns the commits associated with the build in JSON format or a message if no commits are found.")]
        public static async Task<string> GetCommitsByBuildAsync(
            [Description("The name of the organization (e.g., 'msazure').")] string orgName,
            [Description("The build ID to look up.")] string buildId,
            CancellationToken cancellationToken = default)
            => await ExecuteAndLogQueryAsync("BuildChange", orgName, buildId, "No commits found", cancellationToken);

        /// <summary>
        /// Central method to handle execution and logging for simple queries.
        /// </summary>
        private static async Task<string> ExecuteAndLogQueryAsync(
            string tableName,
            string orgName,
            string buildId,
            string noDataMessage,
            CancellationToken cancellationToken = default)
        {
            LogFunctionCall($"DevOpsPlugin.Get{tableName}ByOrgAndBuildAsync", orgName, buildId);

            try
            {
                var result = await KustoHelper
                    .RetrieveDataByOrgAndBuildIdAsync(tableName, orgName, buildId, DefaultClusterKey, cancellationToken)
                    .ConfigureAwait(false);
                LogJson("Tool", result);

                return string.IsNullOrEmpty(result)
                    ? $"{noDataMessage} for Org: {orgName} and BuildId: {buildId}"
                    : result;
            }
            catch (Exception ex)
            {
                LogError("DevOpsPlugin.ExecuteAndLogQueryAsync", $"An error occurred while retrieving data from table '{tableName}': {ex.Message}");
                return CreateErrorResponse($"An internal error occurred while retrieving data from table '{tableName}'.");
            }
        }

        /// <summary>
        /// Provides help information about the DevOpsPlugin.
        /// </summary>
        /// <returns>A JSON string detailing the available DevOpsPlugin functions.</returns>
        public override string Help()
        {
            LogInfo("DevOpsPlugin.Help", "Providing help information for DevOpsPlugin.");

            var helpInfo = new List<object>
            {
                new
                {
                    name = "get_build_info",
                    description = "Retrieves build information by organization name and build ID.",
                    parameters = new List<object>
                    {
                        new
                        {
                            name = "orgName",
                            type = "String",
                            description = "The name of the organization (e.g., 'msazure')."
                        },
                        new
                        {
                            name = "buildId",
                            type = "String",
                            description = "The build ID to look up."
                        },
                        new
                        {
                            name = "cancellationToken",
                            type = "CancellationToken",
                            description = "Optional cancellation token."
                        }
                    },
                    returns = "Returns the build information as a JSON string or a message if no build is found."
                },
                new
                {
                    name = "get_workitem_by_org_build",
                    description = "Retrieves work items linked to a specific build.",
                    parameters = new List<object>
                    {
                        new
                        {
                            name = "orgName",
                            type = "String",
                            description = "The name of the organization (e.g., 'msazure')."
                        },
                        new
                        {
                            name = "buildId",
                            type = "String",
                            description = "The build ID to look up."
                        },
                        new
                        {
                            name = "cancellationToken",
                            type = "CancellationToken",
                            description = "Optional cancellation token."
                        }
                    },
                    returns = "Returns the work items associated with the build in JSON format or a message if no work items are found."
                },
                new
                {
                    name = "get_commits_by_org_build",
                    description = "Retrieves commits linked to a specific build.",
                    parameters = new List<object>
                    {
                        new
                        {
                            name = "orgName",
                            type = "String",
                            description = "The name of the organization (e.g., 'msazure')."
                        },
                        new
                        {
                            name = "buildId",
                            type = "String",
                            description = "The build ID to look up."
                        },
                        new
                        {
                            name = "cancellationToken",
                            type = "CancellationToken",
                            description = "Optional cancellation token."
                        }
                    },
                    returns = "Returns the commits associated with the build in JSON format or a message if no commits are found."
                },
                new
                {
                    name = "get_pull_requests_by_build",
                    description = "Retrieves Pull Requests linked to a specific build by performing a join between BuildChange and PullRequest.",
                    parameters = new List<object>
                    {
                        new
                        {
                            name = "orgName",
                            type = "String",
                            description = "The name of the organization (e.g., 'msazure')."
                        },
                        new
                        {
                            name = "buildId",
                            type = "String",
                            description = "The build ID to look up."
                        },
                        new
                        {
                            name = "cancellationToken",
                            type = "CancellationToken",
                            description = "Optional cancellation token."
                        }
                    },
                    returns = "Returns the pull requests associated with the build in JSON format or a message if no pull requests are found."
                }
            };

            var result = JsonSerializer.Serialize(new { functions = helpInfo }, new JsonSerializerOptions { WriteIndented = true });
            LogInfo("DevOpsPlugin.Help", "Help information provided successfully.");
            return result;
        }
    }
}

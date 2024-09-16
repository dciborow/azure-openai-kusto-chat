namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    using System.ComponentModel;

    using global::Microsoft.SemanticKernel;

    public class ClearwaterPlugin
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
            => await ExecuteAndLogQueryAsync("Build", orgName, buildId, "No build found");

        /// <summary>
        /// Retrieves work items linked to a specific build.
        /// </summary>
        [KernelFunction("get_workitem_by_org_build")]
        [Description("Retrieves work items linked to a specific build.")]
        [return: Description("Returns the work items associated with the build in JSON format or a message if no work items are found.")]
        public async Task<string> GetWorkItemsByBuildAsync(
            [Description("The name of the organization (e.g., 'msazure').")] string orgName,
            [Description("The build ID to look up.")] string buildId)
            => await ExecuteAndLogQueryAsync("BuildWorkItem", orgName, buildId, "No work items found");

        /// <summary>
        /// Retrieves commits linked to a specific build.
        /// </summary>
        [KernelFunction("get_commits_by_org_build")]
        [Description("Retrieves commits linked to a specific build.")]
        [return: Description("Returns the commits associated with the build in JSON format or a message if no commits are found.")]
        public async Task<string> GetCommitsByBuildAsync(
            [Description("The name of the organization (e.g., 'msazure').")] string orgName,
            [Description("The build ID to look up.")] string buildId)
            => await ExecuteAndLogQueryAsync("BuildChange", orgName, buildId, "No commits found");

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
            Logger.LogFunctionCall("ClearwaterPlugin.GetPullRequestsByBuildAsync", orgName, buildId);

            var pullRequests = await KustoHelper.RetrievePullRequestsByBuildIdAsync(orgName, buildId);
            Logger.LogJson("Tool", pullRequests);

            return string.IsNullOrEmpty(pullRequests) ? $"No pull requests found for Org: {orgName} and BuildId: {buildId}" : pullRequests;
        }
        // Central method to handle execution and logging for simple queries
        private async Task<string> ExecuteAndLogQueryAsync(string tableName, string orgName, string buildId, string noDataMessage)
        {
            Logger.LogFunctionCall($"ClearwaterPlugin.Get{tableName}ByOrgAndBuildAsync", orgName, buildId);

            var result = await KustoHelper.RetrieveDataByOrgAndBuildIdAsync(tableName, orgName, buildId);
            Logger.LogJson("Tool", result);

            return string.IsNullOrEmpty(result) ? $"{noDataMessage} for Org: {orgName} and BuildId: {buildId}" : result;
        }
    }
}

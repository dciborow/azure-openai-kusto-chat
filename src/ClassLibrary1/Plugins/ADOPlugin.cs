namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.SemanticKernel;

    public class ADOPlugin : KustoPlugin
    {
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
    }
}

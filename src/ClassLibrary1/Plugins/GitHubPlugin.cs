namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.ComponentModel;
    using System.Reflection;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Kusto.Cloud.Platform.Utils;

    using Microsoft.AzureCore.ReadyToDeploy.Vira.Helpers;
    using Microsoft.SemanticKernel;

    using Octokit;

    /// <summary>
    /// Provides methods to interact with GitHub repositories, such as creating issues.
    /// </summary>
    public class GitHubPlugin : PluginBase
    {
        private readonly GitHubClient _gitHubClient;
        private readonly string _defaultOwner;
        private readonly string _defaultRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubPlugin"/> class.
        /// </summary>
        /// <param name="gitHubToken">GitHub Personal Access Token for authentication.</param>
        /// <param name="defaultOwner">Default repository owner (username or organization).</param>
        /// <param name="defaultRepo">Default repository name.</param>
        public GitHubPlugin(string gitHubToken, string defaultOwner, string defaultRepo)
        {
            if (string.IsNullOrWhiteSpace(gitHubToken))
                throw new ArgumentException("GitHub token cannot be null or empty.", nameof(gitHubToken));

            _defaultOwner = defaultOwner ?? throw new ArgumentNullException(nameof(defaultOwner));
            _defaultRepo = defaultRepo ?? throw new ArgumentNullException(nameof(defaultRepo));

            _gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue("GitHubPlugin"))
            {
                Credentials = new Credentials(gitHubToken)
            };
        }

        /// <summary>
        /// Creates an issue on the specified GitHub repository.
        /// </summary>
        /// <param name="title">The title of the issue.</param>
        /// <param name="body">The body/content of the issue.</param>
        /// <param name="assignees">A comma-separated list of GitHub usernames to assign to the issue.</param>
        /// <param name="labels">A comma-separated list of labels to add to the issue.</param>
        /// <param name="owner">The owner of the repository (optional). Defaults to the configured owner.</param>
        /// <param name="repo">The name of the repository (optional). Defaults to the configured repository.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A JSON string representing the created issue or an error message.</returns>
        [KernelFunction("create_github_issue")]
        [Description("Creates an issue on the specified GitHub repository.")]
        [return: Description("Returns the created issue details in JSON format or an error message.")]
        public async Task<string> CreateIssueAsync(
            [Description("The title of the issue.")] string title,
            [Description("The body/content of the issue.")] string body,
            [Description("A comma-separated list of GitHub usernames to assign to the issue.")] string assignees = "",
            [Description("A comma-separated list of labels to add to the issue.")] string labels = "",
            [Description("The owner of the repository (username or organization). Optional.")] string? owner = null,
            [Description("The name of the repository. Optional.")] string? repo = null,
            CancellationToken cancellationToken = default)
        {
            LogFunctionCall("GitHubPlugin.CreateIssueAsync", title, body, assignees, labels, owner ?? string.Empty, repo ?? string.Empty);

            try
            {
                var newIssue = new NewIssue(title)
                {
                    Body = body
                };

                if (!string.IsNullOrWhiteSpace(assignees))
                {
                    newIssue.Assignees.AddRange(assignees.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                }

                if (!string.IsNullOrWhiteSpace(labels))
                {
                    newIssue.Labels.AddRange(labels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                }

                var createdIssue = await _gitHubClient.Issue.Create(
                    owner ?? _defaultOwner,
                    repo ?? _defaultRepo,
                    newIssue
                ).ConfigureAwait(false);

                var result = JsonSerializer.Serialize(createdIssue, new JsonSerializerOptions { WriteIndented = true });
                LogJson("Tool", result);
                return result;
            }
            catch (NotFoundException)
            {
                string error = "Repository not found. Please ensure the owner and repository names are correct.";
                LogError(error);
                return CreateErrorResponse(error);
            }
            catch (AuthorizationException)
            {
                string error = "Authorization failed. Please check your GitHub token permissions.";
                LogError(error);
                return CreateErrorResponse(error);
            }
            catch (Exception ex)
            {
                string error = $"An error occurred while creating the issue: {ex.Message}";
                LogError(error);
                return CreateErrorResponse(error);
            }
        }

        /// <summary>
        /// Provides help information about the available GitHub kernel functions using reflection.
        /// </summary>
        [KernelFunction("github_help")]
        [Description("Provides detailed information about the available GitHub kernel functions, including descriptions, parameters, and return types.")]
        [return: Description("Returns a JSON string detailing the available GitHub kernel functions.")]
        public string GitHubHelp()
        {
            LogFunctionCall("GitHubPlugin.GitHubHelp");

            var helpInfo = new List<object>();

            // Get all public instance methods of the current class
            var methods = this.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                // Check if the method has the KernelFunctionAttribute
                var kernelFuncAttr = method.GetCustomAttribute<KernelFunctionAttribute>();
                if (kernelFuncAttr == null)
                {
                    continue; // Skip methods without the KernelFunction attribute
                }

                // Get the function name from the KernelFunctionAttribute
                string funcName = kernelFuncAttr.Name!;

                // Get the DescriptionAttribute for the method
                var methodDescAttr = method.GetCustomAttribute<DescriptionAttribute>();
                string funcDescription = methodDescAttr != null ? methodDescAttr.Description : "No description available.";

                // Get parameter descriptions
                var parameters = method.GetParameters().Select(p =>
                {
                    var paramDescAttr = p.GetCustomAttribute<DescriptionAttribute>();
                    return new
                    {
                        name = p.Name,
                        type = p.ParameterType.Name,
                        description = paramDescAttr != null ? paramDescAttr.Description : "No description available."
                    };
                }).ToList();

                // Get return description
                var returnDescAttr = method.ReturnParameter.GetCustomAttribute<DescriptionAttribute>();
                string returnDescription = returnDescAttr != null ? returnDescAttr.Description : "No description available.";

                // Add the function information to the help list
                helpInfo.Add(new
                {
                    name = funcName,
                    description = funcDescription,
                    parameters = parameters,
                    returns = returnDescription
                });
            }

            // Serialize the help information to a formatted JSON string
            var result = JsonSerializer.Serialize(new { functions = helpInfo }, new JsonSerializerOptions { WriteIndented = true });
            LogJson("Help", result);
            return result;
        }

        /// <summary>
        /// Creates a standardized error response in JSON format.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>A JSON string representing the error response.</returns>
        private static new string CreateErrorResponse(string message) =>
            JsonSerializer.Serialize(new { error = true, message }, new JsonSerializerOptions { WriteIndented = true });

        /// <summary>
        /// Logs a function call with the given arguments.
        /// </summary>
        /// <param name="functionName">Name of the function being called.</param>
        /// <param name="args">Arguments passed to the function.</param>
        private new void LogFunctionCall(string functionName, params string[] args) =>
            LoggerHelper.LogFunctionCall($"GitHubPlugin.{functionName}", args);

        /// <summary>
        /// Logs JSON data with a specified label.
        /// </summary>
        /// <param name="label">Label for the JSON data.</param>
        /// <param name="data">The data to log.</param>
        private new void LogJson(string label, string data) =>
            LoggerHelper.LogJson(label, data);

        /// <summary>
        /// Logs an error with exception details.
        /// </summary>
        /// <param name="message">The error message.</param>
        private void LogError(string message) =>
            LoggerHelper.LogError(message);

        public override string Help() => throw new NotImplementedException();
    }
}

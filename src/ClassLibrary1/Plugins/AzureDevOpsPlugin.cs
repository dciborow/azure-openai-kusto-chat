namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.ComponentModel;
    using System.Reflection;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.SemanticKernel;
    using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.WebApi;
    using Microsoft.VisualStudio.Services.WebApi.Patch;
    using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

    /// <summary>
    /// Provides methods to interact with Azure DevOps, such as creating work items.
    /// </summary>
    public class AzureDevOpsPlugin : PluginBase
    {
        private readonly WorkItemTrackingHttpClient _workItemClient;
        private readonly string _defaultProject;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureDevOpsPlugin"/> class.
        /// </summary>
        /// <param name="personalAccessToken">Azure DevOps Personal Access Token for authentication.</param>
        /// <param name="organizationUrl">The Azure DevOps organization URL (e.g., https://dev.azure.com/yourorganization).</param>
        /// <param name="defaultProject">The default project name where work items will be created.</param>
        public AzureDevOpsPlugin(string personalAccessToken, string organizationUrl, string defaultProject)
        {
            if (string.IsNullOrWhiteSpace(personalAccessToken))
                throw new ArgumentException("Personal Access Token cannot be null or empty.", nameof(personalAccessToken));

            if (string.IsNullOrWhiteSpace(organizationUrl))
                throw new ArgumentException("Organization URL cannot be null or empty.", nameof(organizationUrl));

            if (string.IsNullOrWhiteSpace(defaultProject))
                throw new ArgumentException("Default project cannot be null or empty.", nameof(defaultProject));

            _defaultProject = defaultProject;

            var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
            var connection = new VssConnection(new Uri(organizationUrl), credentials);
            _workItemClient = connection.GetClient<WorkItemTrackingHttpClient>();
        }

        /// <summary>
        /// Executes the primary function of the AzureDevOpsPlugin.
        /// For this plugin, ExecuteAsync is designed to create a work item.
        /// </summary>
        /// <param name="parameters">Parameters required for execution: workItemType, title, description, [assignedTo], [tags], [project].</param>
        /// <returns>A JSON string representing the created work item or an error message.</returns>
        public override async Task<string> ExecuteAsync(params string[] parameters)
        {
            if (parameters.Length < 3)
            {
                return CreateErrorResponse("Insufficient parameters. Required: workItemType, title, description.");
            }

            string workItemType = parameters[0];
            string title = parameters[1];
            string description = parameters[2];
            string assignedTo = parameters.Length > 3 ? parameters[3] : "";
            string tags = parameters.Length > 4 ? parameters[4] : "";
            string project = parameters.Length > 5 ? parameters[5] : _defaultProject;

            return await CreateWorkItemAsync(workItemType, title, description, assignedTo, tags, project);
        }

        /// <summary>
        /// Creates a work item in the specified Azure DevOps project.
        /// </summary>
        /// <param name="workItemType">The type of work item to create (e.g., 'Bug', 'Task', 'User Story').</param>
        /// <param name="title">The title of the work item.</param>
        /// <param name="description">The description of the work item.</param>
        /// <param name="assignedTo">The email address of the user to assign the work item to. Optional.</param>
        /// <param name="tags">A comma-separated list of tags to add to the work item. Optional.</param>
        /// <param name="project">The name of the Azure DevOps project. Optional. Defaults to the configured project.</param>
        /// <returns>A JSON string representing the created work item or an error message.</returns>
        [KernelFunction("create_work_item")]
        [Description("Creates a work item in the specified Azure DevOps project.")]
        [return: Description("Returns the created work item details in JSON format or an error message.")]
        public async Task<string> CreateWorkItemAsync(
            [Description("The type of work item to create (e.g., 'Bug', 'Task', 'User Story').")] string workItemType,
            [Description("The title of the work item.")] string title,
            [Description("The description of the work item.")] string description,
            [Description("The email address of the user to assign the work item to. Optional.")] string assignedTo = "",
            [Description("A comma-separated list of tags to add to the work item. Optional.")] string tags = "",
            [Description("The name of the Azure DevOps project. Optional.")] string? project = null,
            CancellationToken cancellationToken = default)
        {
            LogFunctionCall("AzureDevOpsPlugin.CreateWorkItemAsync", workItemType, title, description, assignedTo, tags, project ?? _defaultProject);

            if (string.IsNullOrWhiteSpace(workItemType))
            {
                return CreateErrorResponse("The 'workItemType' parameter cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                return CreateErrorResponse("The 'title' parameter cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return CreateErrorResponse("The 'description' parameter cannot be null or empty.");
            }

            string targetProject = project ?? _defaultProject;

            try
            {
                var patchDocument = new JsonPatchDocument();

                // Add Title
                patchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Title",
                    Value = title
                });

                // Add Description
                patchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Description",
                    Value = description
                });

                // Add Assigned To if provided
                if (!string.IsNullOrWhiteSpace(assignedTo))
                {
                    patchDocument.Add(new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.AssignedTo",
                        Value = assignedTo
                    });
                }

                // Add Tags if provided
                if (!string.IsNullOrWhiteSpace(tags))
                {
                    patchDocument.Add(new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Tags",
                        Value = tags.Replace(",", ";")
                    });
                }

                var createdWorkItem = await _workItemClient.CreateWorkItemAsync(
                    patchDocument,
                    targetProject,
                    workItemType,
                    validateOnly: false,
                    bypassRules: false,
                    suppressNotifications: false,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                var result = JsonSerializer.Serialize(createdWorkItem, new JsonSerializerOptions { WriteIndented = true });
                LogJson("AzureDevOpsPlugin.CreateWorkItemAsync", result);
                LogInfo("AzureDevOpsPlugin.CreateWorkItemAsync", "Work item created successfully.");
                return result;
            }
            catch (Exception ex)
            {
                string error = $"An error occurred while creating the work item: {ex.Message}";
                LogError("AzureDevOpsPlugin.CreateWorkItemAsync", error);
                return CreateErrorResponse(error);
            }
        }

        /// <summary>
        /// Provides help information about the available Azure DevOps kernel functions using reflection.
        /// </summary>
        /// <returns>A JSON string detailing the available Azure DevOps kernel functions.</returns>
        [KernelFunction("azuredevops_help")]
        [Description("Provides detailed information about the available Azure DevOps kernel functions, including descriptions, parameters, and return types.")]
        [return: Description("Returns a JSON string detailing the available Azure DevOps kernel functions.")]
        public override string Help()
        {
            LogInfo("AzureDevOpsPlugin.Help", "Providing help information for AzureDevOpsPlugin.");

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
                string funcName = kernelFuncAttr.Name;

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
            LogInfo("AzureDevOpsPlugin.Help", "Help information provided successfully.");
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
            var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });
            var logLine = $"{json}{Environment.NewLine}";
            await File.AppendAllTextAsync(filePath, logLine);
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
    }
}

namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Microsoft.SemanticKernel;

    /// <summary>
    /// Represents a plugin for logging user feedback, bugs, and internal errors or kernel improvements by appending them to files on disk.
    /// </summary>
    public class MetaPlugin : PluginBase
    {
        private readonly string _logsDirectory;
        private readonly string _userFeedbackFilePath;
        private readonly string _bugsFilePath;
        private readonly string _errorsFilePath;
        private readonly string _kernelImprovementsFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaPlugin"/> class.
        /// </summary>
        /// <param name="logsDirectory">The directory where log files will be stored. If not provided, defaults to 'Logs' directory in the application root.</param>
        public MetaPlugin(string? logsDirectory = null)
        {
            _logsDirectory = logsDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            try
            {
                // Ensure the logs directory exists
                Directory.CreateDirectory(_logsDirectory);
            }
            catch (Exception ex)
            {
                LogError("MetaPlugin.Constructor", $"Failed to create logs directory '{_logsDirectory}': {ex.Message}");
                throw new DirectoryNotFoundException($"Unable to create or access the logs directory at '{_logsDirectory}'.", ex);
            }

            // Define file paths
            _userFeedbackFilePath = Path.Combine(_logsDirectory, "user_feedback.log");
            _bugsFilePath = Path.Combine(_logsDirectory, "bugs.log");
            _errorsFilePath = Path.Combine(_logsDirectory, "errors.log");
            _kernelImprovementsFilePath = Path.Combine(_logsDirectory, "kernel_improvements.log");
        }

        /// <summary>
        /// Executes the primary function of the MetaPlugin.
        /// Designed to save user feedback, bugs, errors, or kernel improvements based on parameters.
        /// </summary>
        /// <param name="parameters">Parameters required for execution: action (save_user_feedback, save_bug, save_error, save_kernel_improvement), content.</param>
        /// <returns>A JSON string indicating the result of the logging operation.</returns>
        public override async Task<string> ExecuteAsync(params string[] parameters)
        {
            LogFunctionCall("MetaPlugin.ExecuteAsync", parameters);

            if (parameters.Length < 2)
            {
                return CreateErrorResponse("Insufficient parameters. Required: action, content.");
            }

            string action = parameters[0].ToLower();
            string content = parameters[1];

            return action switch
            {
                "save_user_feedback" => await SaveUserFeedbackAsync(content),
                "save_bug" => await SaveBugAsync(content),
                "save_error" => await SaveErrorAsync(content),
                "save_kernel_improvement" => await SaveKernelImprovementAsync(content),
                _ => CreateErrorResponse($"Unknown action '{action}'. Valid actions are: save_user_feedback, save_bug, save_error, save_kernel_improvement.")
            };
        }

        /// <summary>
        /// Saves user feedback by appending it to the user feedback log file.
        /// </summary>
        /// <param name="feedback">The feedback content to save.</param>
        /// <returns>A confirmation message or an error message.</returns>
        [KernelFunction("save_user_feedback")]
        [Description("Saves user feedback by appending it to the user feedback log file.")]
        [return: Description("Returns a confirmation message if successful or an error message.")]
        public async Task<string> SaveUserFeedbackAsync(string feedback)
        {
            LogFunctionCall("MetaPlugin.SaveUserFeedbackAsync", feedback);

            if (string.IsNullOrWhiteSpace(feedback))
            {
                return CreateErrorResponse("Feedback content cannot be null or empty.");
            }

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Type = "UserFeedback",
                Content = feedback
            };

            try
            {
                await AppendLogAsync(_userFeedbackFilePath, logEntry);
                LogInfo("MetaPlugin.SaveUserFeedbackAsync", "User feedback saved successfully.");
                return $"User feedback saved successfully at {logEntry.Timestamp:O}.";
            }
            catch (Exception ex)
            {
                string error = $"An error occurred while saving user feedback: {ex.Message}";
                LogError("MetaPlugin.SaveUserFeedbackAsync", error);
                return CreateErrorResponse(error);
            }
        }

        /// <summary>
        /// Saves a bug report by appending it to the bugs log file.
        /// </summary>
        /// <param name="bugDescription">The description of the bug to save.</param>
        /// <returns>A confirmation message or an error message.</returns>
        [KernelFunction("save_bug")]
        [Description("Saves a bug report by appending it to the bugs log file.")]
        [return: Description("Returns a confirmation message if successful or an error message.")]
        public async Task<string> SaveBugAsync(string bugDescription)
        {
            LogFunctionCall("MetaPlugin.SaveBugAsync", bugDescription);

            if (string.IsNullOrWhiteSpace(bugDescription))
            {
                return CreateErrorResponse("Bug description cannot be null or empty.");
            }

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Type = "Bug",
                Content = bugDescription
            };

            try
            {
                await AppendLogAsync(_bugsFilePath, logEntry);
                LogInfo("MetaPlugin.SaveBugAsync", "Bug report saved successfully.");
                return $"Bug report saved successfully at {logEntry.Timestamp:O}.";
            }
            catch (Exception ex)
            {
                string error = $"An error occurred while saving bug report: {ex.Message}";
                LogError("MetaPlugin.SaveBugAsync", error);
                return CreateErrorResponse(error);
            }
        }

        /// <summary>
        /// Saves an internal error by appending it to the errors log file.
        /// </summary>
        /// <param name="errorMessage">The error message to save.</param>
        /// <returns>A confirmation message or an error message.</returns>
        [KernelFunction("save_error")]
        [Description("Saves an internal error by appending it to the errors log file.")]
        [return: Description("Returns a confirmation message if successful or an error message.")]
        public async Task<string> SaveErrorAsync(string errorMessage)
        {
            LogFunctionCall("MetaPlugin.SaveErrorAsync", errorMessage);

            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return CreateErrorResponse("Error message cannot be null or empty.");
            }

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Type = "Error",
                Content = errorMessage
            };

            try
            {
                await AppendLogAsync(_errorsFilePath, logEntry);
                LogInfo("MetaPlugin.SaveErrorAsync", "Error message saved successfully.");
                return $"Error message saved successfully at {logEntry.Timestamp:O}.";
            }
            catch (Exception ex)
            {
                string error = $"An error occurred while saving error message: {ex.Message}";
                LogError("MetaPlugin.SaveErrorAsync", error);
                return CreateErrorResponse(error);
            }
        }

        /// <summary>
        /// Saves a kernel improvement suggestion by appending it to the kernel improvements log file.
        /// </summary>
        /// <param name="improvement">The kernel improvement suggestion to save.</param>
        /// <returns>A confirmation message or an error message.</returns>
        [KernelFunction("save_kernel_improvement")]
        [Description("Saves a kernel improvement suggestion by appending it to the kernel improvements log file.")]
        [return: Description("Returns a confirmation message if successful or an error message.")]
        public async Task<string> SaveKernelImprovementAsync(string improvement)
        {
            LogFunctionCall("MetaPlugin.SaveKernelImprovementAsync", improvement);

            if (string.IsNullOrWhiteSpace(improvement))
            {
                return CreateErrorResponse("Kernel improvement content cannot be null or empty.");
            }

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Type = "KernelImprovement",
                Content = improvement
            };

            try
            {
                await AppendLogAsync(_kernelImprovementsFilePath, logEntry);
                LogInfo("MetaPlugin.SaveKernelImprovementAsync", "Kernel improvement saved successfully.");
                return $"Kernel improvement saved successfully at {logEntry.Timestamp:O}.";
            }
            catch (Exception ex)
            {
                string error = $"An error occurred while saving kernel improvement: {ex.Message}";
                LogError("MetaPlugin.SaveKernelImprovementAsync", error);
                return CreateErrorResponse(error);
            }
        }

        /// <summary>
        /// Provides help information for the MetaPlugin.
        /// </summary>
        /// <returns>A JSON string detailing the available MetaPlugin functions.</returns>
        [KernelFunction("metaplugin_help")]
        [Description("Provides detailed information about the available MetaPlugin functions, including descriptions, parameters, and return types.")]
        [return: Description("Returns a JSON string detailing the available MetaPlugin functions.")]
        public override string Help()
        {
            LogFunctionCall("MetaPlugin.Help");

            var helpInfo = new List<object>
            {
                new
                {
                    name = "save_user_feedback",
                    description = "Saves user feedback by appending it to the user feedback log file.",
                    parameters = new List<object>
                    {
                        new
                        {
                            name = "feedback",
                            type = "String",
                            description = "The feedback content to save."
                        }
                    },
                    returns = "Returns a confirmation message if successful or an error message."
                },
                new
                {
                    name = "save_bug",
                    description = "Saves a bug report by appending it to the bugs log file.",
                    parameters = new List<object>
                    {
                        new
                        {
                            name = "bugDescription",
                            type = "String",
                            description = "The description of the bug to save."
                        }
                    },
                    returns = "Returns a confirmation message if successful or an error message."
                },
                new
                {
                    name = "save_error",
                    description = "Saves an internal error by appending it to the errors log file.",
                    parameters = new List<object>
                    {
                        new
                        {
                            name = "errorMessage",
                            type = "String",
                            description = "The error message to save."
                        }
                    },
                    returns = "Returns a confirmation message if successful or an error message."
                },
                new
                {
                    name = "save_kernel_improvement",
                    description = "Saves a kernel improvement suggestion by appending it to the kernel improvements log file.",
                    parameters = new List<object>
                    {
                        new
                        {
                            name = "improvement",
                            type = "String",
                            description = "The kernel improvement suggestion to save."
                        }
                    },
                    returns = "Returns a confirmation message if successful or an error message."
                }
            };

            var result = JsonSerializer.Serialize(new { functions = helpInfo }, new JsonSerializerOptions { WriteIndented = true });
            LogJson("MetaPlugin.Help", result);
            LogInfo("MetaPlugin.Help", "Help information provided successfully.");
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
                LogJson("MetaPlugin.AppendLogAsync", $"Appended log to '{filePath}'.");
            }
            catch (Exception ex)
            {
                LogError("MetaPlugin.AppendLogAsync", $"Failed to append log to '{filePath}': {ex.Message}");
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
    }
}

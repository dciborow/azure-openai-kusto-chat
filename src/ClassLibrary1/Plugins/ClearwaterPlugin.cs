namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Microsoft.SemanticKernel;

    /// <summary>
    /// Represents a plugin for providing aggregated help information about all available plugins.
    /// </summary>
    public class ClearwaterPlugin : PluginBase
    {
        /// <summary>
        /// Executes the primary function of the ClearwaterPlugin.
        /// Not implemented as ClearwaterPlugin primarily provides help information.
        /// </summary>
        /// <param name="parameters">Parameters for execution.</param>
        /// <returns>Not implemented.</returns>
        public override Task<string> ExecuteAsync(params string[] parameters) => throw new NotImplementedException();

        /// <summary>
        /// Provides aggregated help information for all plugins in the Clearwater namespace.
        /// </summary>
        /// <returns>A JSON string detailing the available plugins and their functionalities.</returns>
        [KernelFunction("help")]
        [Description("Provides aggregated help information for all available plugins.")]
        [return: Description("Returns the aggregated help information as a JSON string.")]
        public override string Help()
        {
            LogFunctionCall("ClearwaterPlugin.Help", "Aggregating help information from all plugins.");

            try
            {
                // Define the target namespace
                string targetNamespace = "Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins";

                // Get the assembly where the plugins are defined
                Assembly assembly = Assembly.GetExecutingAssembly();

                // Find all public, non-abstract classes within the target namespace that implement IPlugin
                var pluginTypes = assembly.GetTypes().Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.IsPublic &&
                    t.Namespace == targetNamespace &&
                    typeof(IPlugin).IsAssignableFrom(t)
                );

                var allHelpInfo = new List<object>();

                foreach (var type in pluginTypes)
                {
                    // Attempt to create an instance of the plugin using a factory method
                    // This factory should handle the instantiation logic based on the plugin type
                    if (!PluginFactory.TryCreatePlugin(type, out IPlugin? pluginInstance))
                    {
                        string errorMsg = $"Cannot instantiate plugin '{type.Name}'. Required parameters are missing or invalid.";
                        LogError("ClearwaterPlugin.Help", errorMsg);
                        continue; // Skip this plugin and continue with others
                    }

                    // Invoke the Help method
                    var helpResult = pluginInstance.Help();
                    if (string.IsNullOrWhiteSpace(helpResult))
                        continue;

                    // Attempt to parse the helpResult as JSON
                    try
                    {
                        var parsedHelp = JsonSerializer.Deserialize<JsonElement>(helpResult);
                        allHelpInfo.Add(new
                        {
                            plugin = type.Name,
                            help = parsedHelp
                        });
                    }
                    catch (JsonException)
                    {
                        // If helpResult is not JSON, include it as a plain string
                        allHelpInfo.Add(new
                        {
                            plugin = type.Name,
                            help = helpResult
                        });
                    }
                }

                // Serialize the aggregated help information
                var aggregatedHelp = JsonSerializer.Serialize(new { plugins = allHelpInfo }, new JsonSerializerOptions { WriteIndented = true });

                LogFunctionCall("ClearwaterPlugin.Help", "Aggregated help information provided successfully.");
                return aggregatedHelp;
            }
            catch (Exception ex)
            {
                string error = $"An error occurred while aggregating help information: {ex.Message}";
                LogError("ClearwaterPlugin.Help", error);
                return CreateErrorResponse(error);
            }
        }
    }

    /// <summary>
    /// Factory class responsible for creating plugin instances with the required parameters.
    /// </summary>
    public static class PluginFactory
    {
        /// <summary>
        /// Attempts to create an instance of the specified plugin type.
        /// </summary>
        /// <param name="pluginType">The type of the plugin to instantiate.</param>
        /// <param name="pluginInstance">The instantiated plugin, if successful.</param>
        /// <returns>True if instantiation was successful; otherwise, false.</returns>
        public static bool TryCreatePlugin(Type pluginType, out IPlugin? pluginInstance)
        {
            pluginInstance = null;

            try
            {
                if (pluginType == typeof(MetaPlugin))
                {
                    // Provide necessary parameters for MetaPlugin
                    // Example: Assume logsDirectory is optional and can be null
                    pluginInstance = new MetaPlugin();
                    return true;
                }
                else if (pluginType == typeof(AzureDevOpsPlugin))
                {
                    // Provide necessary parameters for AzureDevOpsPlugin
                    // Replace the placeholders with actual values or retrieve them from configuration
                    string personalAccessToken = "YOUR_AZURE_DEVOPS_PAT";
                    string organizationUrl = "https://dev.azure.com/yourorganization";
                    string defaultProject = "YourDefaultProject";

                    pluginInstance = new AzureDevOpsPlugin(personalAccessToken, organizationUrl, defaultProject);
                    return true;
                }
                else if (pluginType == typeof(GitHubPlugin))
                {
                    // Provide necessary parameters for GitHubPlugin
                    // Replace the placeholders with actual values or retrieve them from configuration
                    string gitHubToken = "YOUR_GITHUB_TOKEN";
                    string defaultOwner = "yourusername";
                    string defaultRepo = "yourrepository";

                    pluginInstance = new GitHubPlugin(gitHubToken, defaultOwner, defaultRepo);
                    return true;
                }
                // Add additional plugin instantiation logic here as needed
                else
                {
                    // Attempt to find a parameterless constructor
                    var ctor = pluginType.GetConstructor(Type.EmptyTypes);
                    if (ctor != null)
                    {
                        pluginInstance = (IPlugin?)Activator.CreateInstance(pluginType);
                        return pluginInstance != null;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log instantiation errors if necessary
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Error - PluginFactory.TryCreatePlugin] Failed to instantiate plugin '{pluginType.Name}': {ex.Message}");
                Console.ResetColor();
                return false;
            }

            return false;
        }
    }
}

namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System.ComponentModel;

    using Microsoft.SemanticKernel;

    /// <summary>
    /// Represents a plugin for providing aggregated help information about all available plugins.
    /// </summary>
    public class ClearwaterPlugin : PluginBase
    {
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

            return "The ClearwaterPlugin provides aggregated help information for all available plugins.";
        }
    }
}

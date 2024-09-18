namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    /// <summary>
    /// Defines the contract for all plugins.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Provides help information about the plugin's functionalities.
        /// </summary>
        /// <returns>A JSON-formatted string detailing available functions and their usage.</returns>
        string Help();
    }
}

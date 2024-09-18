namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the contract for all plugins.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Initializes the plugin with necessary dependencies or configurations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Provides help information about the plugin's functionalities.
        /// </summary>
        /// <returns>A JSON-formatted string detailing available functions and their usage.</returns>
        string Help();
    }
}

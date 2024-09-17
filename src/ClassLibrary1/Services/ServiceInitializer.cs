namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Services
{
    using Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins;

    /// <summary>
    /// A class to initialize services.
    /// </summary>
    public class ServiceInitializer
    {
        /// <summary>
        /// Initializes the ClearwaterChatService.
        /// </summary>
        /// <param name="deploymentName">The deployment name for Azure OpenAI.</param>
        /// <param name="endpoint">The endpoint for Azure OpenAI.</param>
        /// <returns>An instance of ClearwaterChatService.</returns>
        public ClearwaterChatService InitializeClearwaterChatService(string deploymentName, string endpoint)
        {
            return new ClearwaterChatService(deploymentName, endpoint);
        }

        /// <summary>
        /// Initializes other services.
        /// </summary>
        public void InitializeOtherServices()
        {
            // Add initialization logic for other services here
        }
    }
}

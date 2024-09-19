namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    using global::Microsoft.SemanticKernel;
    using global::Microsoft.SemanticKernel.ChatCompletion;
    using global::Microsoft.SemanticKernel.Connectors.OpenAI;

    using Microsoft.AzureCore.ReadyToDeploy.Vira.Helpers;
    using Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins;

    /// <summary>
    /// A service for managing chat interactions using Azure OpenAI with persistent chat history.
    /// </summary>
    public class ClearwaterChatService
    {
        private readonly Kernel _kernel;
        private readonly ChatHistory _chatHistory;

        // Initialize chat history in the constructor
        public ClearwaterChatService(string deploymentName, string endpoint)
        {
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, CredentialHelper.CreateChainedCredential());
            builder.Plugins.AddFromType<MetaPlugin>();
            builder.Plugins.AddFromType<KustoPlugin>();
            builder.Plugins.AddFromType<SafeFlyPlugin>();

            builder.Plugins.AddFromType<DevOpsPlugin>();

            ConfigureAzureSearch(builder);
            _kernel = builder.Build();

            // Initialize the chat history to persist across multiple interactions
            _chatHistory = new ChatHistory("How can I assist you today?");
        }

        public async Task<string> GetChatResponseAsync(string userId, string userInput)
            => await GetChatResponseAsync(userInput);

        // Method now reuses chat history
        public async Task<string> GetChatResponseAsync(string userInput)
        {
            // Add the user's message to the existing chat history
            _chatHistory.AddUserMessage(userInput);

            var openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            try
            {
                var result = await chatCompletionService.GetChatMessageContentAsync(
                    _chatHistory,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: _kernel
                );

                _chatHistory.AddAssistantMessage(result.Content!);

                return result.Content!;
            }
            catch (Exception)
            {
                // Log the exception and return a generic error message
                _chatHistory.AddAssistantMessage("I'm sorry, I encountered an error while processing your request. Please try again later.");
                return "I'm sorry, I encountered an error while processing your request. Please try again later.";
            }
        }

#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private static void ConfigureAzureSearch(IKernelBuilder builder) =>
            builder.AddAzureAISearchVectorStore(
                new Uri("https://appfdocsindex.search.windows.net"),
                CredentialHelper.CreateChainedCredential());
#pragma warning restore SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    }
}
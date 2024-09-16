namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    using Azure.Core;
    using Azure.Identity;

    using global::Microsoft.SemanticKernel;
    using global::Microsoft.SemanticKernel.ChatCompletion;
    using global::Microsoft.SemanticKernel.Connectors.OpenAI;

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
            builder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, CreateChainedCredential());
            builder.Plugins.AddFromType<Plugins.ClearwaterPlugin>();
            builder.Plugins.AddFromType<Plugins.KustoPlugin>();
            builder.Plugins.AddFromType<Plugins.DevOpsPlugin>();

            _kernel = builder.Build();

            // Initialize the chat history to persist across multiple interactions
            _chatHistory = new ChatHistory("How can I assist you today?");
        }

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
            var result = await chatCompletionService.GetChatMessageContentAsync(
                _chatHistory,
                executionSettings: openAIPromptExecutionSettings,
                kernel: _kernel
            );

            // Add assistant's response to the chat history
            _chatHistory.AddAssistantMessage(result.Content!);

            return result.Content!;
        }

        /// <summary>
        /// Creates a ChainedTokenCredential to authenticate with Azure.
        /// </summary>
        internal static TokenCredential CreateChainedCredential()
            => new ChainedTokenCredential(
                new VisualStudioCredential(),
                new VisualStudioCodeCredential(),
                new AzureCliCredential(),
                new DefaultAzureCredential()
            );
    }
}

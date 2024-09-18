namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using Microsoft.AzureCore.ReadyToDeploy.Vira.Helpers;
    using Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.ChatCompletion;
    using Microsoft.SemanticKernel.Connectors.OpenAI;

    public class ClearwaterChatService
    {
        private readonly Kernel _kernel;
        private readonly ConcurrentDictionary<string, ChatHistory> _chatHistories;

        public ClearwaterChatService(string deploymentName, string endpoint)
        {
            var builder = Kernel
                .CreateBuilder()
                .AddAzureOpenAIChatCompletion(deploymentName, endpoint, CredentialHelper.CreateChainedCredential());

            builder.Plugins.AddFromType<MetaPlugin>();
            builder.Plugins.AddFromType<KustoPlugin>();
            builder.Plugins.AddFromType<SafeFlyPlugin>();
            builder.Plugins.AddFromType<DevOpsPlugin>();

            _kernel = builder.Build();
            _chatHistories = new ConcurrentDictionary<string, ChatHistory>();
        }

        public async Task<string> GetChatResponseAsync(string userId, string userInput)
        {
            var chatHistory = _chatHistories.GetOrAdd(userId, _ => new ChatHistory("How can I assist you today?"));
            chatHistory.AddUserMessage(userInput);

            var openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            var chatCompletionService = _kernel.Services.GetRequiredService<IChatCompletionService>();

            var result = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                openAIPromptExecutionSettings);

            chatHistory.AddAssistantMessage(result.Content);

            return result.Content;
        }
    }
}

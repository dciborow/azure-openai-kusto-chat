namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    /// <summary>
    /// Entry point for the Clearwater Chat REPL application.
    /// Initializes the chat service and processes user input in a loop.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point for the Clearwater Chat REPL application.
        /// Initializes the chat service and processes user input in a loop.
        /// </summary>
        public static async Task Main(string[] args)
        {
            // Azure OpenAI API Setup
            string deploymentName = "gpt-4o-mini";
            string endpoint = "https://gpt-review.openai.azure.com";

            var chatService = new ClearwaterChatService(deploymentName, endpoint);

            // REPL loop for interactive chat
            Console.WriteLine("Welcome to the Clearwater Chat REPL! Try asking me about ADO Org Build Id. Type 'exit' to quit.");

            while (true)
            {
                Console.ResetColor();
                Console.Write("You: ");
                string userInput = Console.ReadLine()!;

                if (userInput?.ToLower() == "exit")
                {
                    break;
                }

                var response = await chatService.GetChatResponseAsync(userInput!);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Assistant > " + response);
            }
        }
    }
}

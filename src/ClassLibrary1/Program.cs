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
            var chatService2 = new ClearwaterChatService(deploymentName, endpoint);

            // REPL loop for interactive chat
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\t\t\t\tWelcome to the Clearwater Chat REPL! Try asking me about ADO Org Build Id. Type 'exit' to quit.");

            while (true)
            {
                Console.ResetColor();
                Console.WriteLine(lineSeperator);
                Console.Write("You: ");
                string userInput = Console.ReadLine()!;

                if (userInput?.ToLower() == "exit")
                {
                    break;
                }

                Console.WriteLine(lineSeperator);

                var response = await chatService.GetChatResponseAsync("user", userInput!);
                var cleanedResponse = await chatService2.GetChatResponseAsync("user", $"See if you can improve the previous response before we send it to the user, only respond with the updated text. \n\nQuestion: {userInput!}, Answer:{response}");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(lineSeperator);

                Console.WriteLine("Assistant > " + cleanedResponse);
                Console.WriteLine(lineSeperator + "\n");

            }
        }

        private static readonly string lineSeperator = "\t\t==================================================================================================================================";
    }
}

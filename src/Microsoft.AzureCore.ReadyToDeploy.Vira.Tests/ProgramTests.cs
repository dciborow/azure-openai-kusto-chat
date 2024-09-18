namespace ChatServiceTests
{
    using System.Threading.Tasks;

    using Microsoft.AzureCore.ReadyToDeploy.Vira;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProgramTests
    {
        private static ClearwaterChatService? chatService;

        [ClassInitialize]
        public static void Initialize(TestContext context)
            => chatService = new ClearwaterChatService("gpt-4o-mini", "https://gpt-review.openai.azure.com");

        [TestMethod]
        public async Task AskAboutRequestAndBuild_ShouldReturnFoundResponses()
        {
            // Arrange
            string question = "What was that newest SafeFly Request for SQL Control Plane before Sept 20 2024?";

            // Act
            var response = await chatService.GetChatResponseAsync("user", question);

            // Assert
            Assert.IsTrue(response.Contains("4282"), $"Response should contain the SafeFly request ID. Response: {response}");
        }
    }
}

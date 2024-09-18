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
            string question = "Tell me about the latest SafeFly Request and Build ID for SQL Control Plane.";

            // Act
            var response = await chatService.GetChatResponseAsync(question);

            // Assert
            Assert.IsTrue(response.Contains("SafeFly Request"), "Response should mention SafeFly Request.");
            Assert.IsTrue(response.Contains("4282"), "Response should mention Build ID.");
        }
    }
}

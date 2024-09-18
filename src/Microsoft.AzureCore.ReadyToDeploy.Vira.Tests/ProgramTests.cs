namespace ChatServiceTests
{
    using System.Threading.Tasks;

    using Microsoft.AzureCore.ReadyToDeploy.Vira;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProgramTests
    {
        [TestMethod]
        public async Task AskAboutRequestAndBuild_ShouldReturnFoundResponses()
        {
            // Arrange
            var mockChatService = new ClearwaterChatService("gpt-4o-mini", "https://gpt-review.openai.azure.com");
            string question = "Tell me about the latest SafeFly Request and Build ID for SQL Control Plane.";
            string expectedResponse = "Latest SafeFly request is found with Build ID 140882654.";

            // Act
            var response = await mockChatService.GetChatResponseAsync(question);

            // Assert
            Assert.IsTrue(response.Contains("SafeFly Request"), "Response should mention SafeFly Request.");
            Assert.IsTrue(response.Contains("Build ID"), "Response should mention Build ID.");
            Assert.AreEqual(expectedResponse, response, "Response does not match the expected output.");
        }
    }
}

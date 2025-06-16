namespace Microsoft.AzureCore.ReadyToDeploy.ViraUI.Server.Controllers
{
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AzureCore.ReadyToDeploy.Vira;

    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ClearwaterChatService _chatService;

        public ChatController(ClearwaterChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatMessage message)
        {
            var response = await _chatService.GetChatResponseAsync("user", message.UserInput);
            return Ok(new { Response = response });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class SemanticKernelPluginController : ControllerBase
    {
        private readonly ClearwaterChatService _chatService;

        public SemanticKernelPluginController(ClearwaterChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Processes a user input and returns a semantic response.
        /// </summary>
        /// <param name="request">The chat message from the user.</param>
        /// <returns>A semantic response based on the user input.</returns>
        [HttpPost("Process")]
        [ProducesResponseType(typeof(SemanticResponse), 200)]
        public async Task<IActionResult> Process([FromBody] ChatMessage request)
        {
            var response = await _chatService.GetChatResponseAsync("user", request.UserInput);
            return Ok(new SemanticResponse { Response = response });
        }
    }

    public class ChatMessage
    {
        public required string UserInput { get; set; }
    }

    public class SemanticResponse
    {
        public required string Response { get; set; }
    }
}

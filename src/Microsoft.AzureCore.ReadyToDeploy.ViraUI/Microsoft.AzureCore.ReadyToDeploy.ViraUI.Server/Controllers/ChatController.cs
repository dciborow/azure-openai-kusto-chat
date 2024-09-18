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

    public class ChatMessage
    {
        public required string UserInput { get; set; }
    }
}

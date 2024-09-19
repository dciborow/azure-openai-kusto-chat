using System.Threading.Tasks;

namespace Microsoft.AzureCore.ReadyToDeploy.ViraUI.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssistantController : ControllerBase
    {
        private readonly ClearwaterChatService _chatService;

        public AssistantController(ClearwaterChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AssistantRequest request)
        {
            var response = await _chatService.GetChatResponseAsync(request.UserId, request.UserInput);
            return Ok(new { Response = response });
        }
    }

    public class AssistantRequest
    {
        public string UserId { get; set; }
        public string UserInput { get; set; }
    }
}

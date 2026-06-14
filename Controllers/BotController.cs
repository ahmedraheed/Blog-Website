using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BlogApp.Services;

namespace BlogApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotController : ControllerBase
    {
        private readonly RAGService _ragService;

        public BotController(RAGService ragService)
        {
            _ragService = ragService;
        }

        public class ChatMessageDto
        {
            public string Role { get; set; } = "user";
            public string Text { get; set; } = string.Empty;
        }

        public class ChatRequest
        {
            public System.Collections.Generic.List<ChatMessageDto> Messages { get; set; } = new System.Collections.Generic.List<ChatMessageDto>();
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest req)
        {
            if (req.Messages == null || req.Messages.Count == 0) return BadRequest("Message cannot be empty.");
            
            var answer = await _ragService.ChatAsync(req.Messages);
            return Ok(new { text = answer });
        }
    }
}

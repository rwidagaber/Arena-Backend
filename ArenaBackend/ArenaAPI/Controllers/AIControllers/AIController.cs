using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArenaApi.Controllers.AIControllers
{
    [ApiController]
    [Route("api/chat")]
    //[Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(
            [FromBody] SendMessageDto dto)
        {
            var reply = await _chatService
                .SendMessageAsync(dto.MemberProfileId, dto.Message);
            return Ok(new ChatResponseDto { Reply = reply });
        }

        [HttpGet("history/{memberProfileId}")]
        public async Task<IActionResult> GetHistory(Guid memberProfileId)
        {
            var history = await _chatService.GetHistoryAsync(memberProfileId);
            return Ok(history);
        }
    }
}

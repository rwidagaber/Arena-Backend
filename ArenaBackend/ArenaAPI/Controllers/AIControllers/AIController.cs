using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using ArenaApi.Configurations.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/chat")]
[Authorize]
[TypeFilter(typeof(RequireAIPlanFilter))]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    // Send message (new or existing conversation)
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        try
        {
            var result = await _chatService.SendMessageAsync(
                dto.MemberProfileId,
                dto.ConversationId,
                dto.Message);
            return Ok(result);
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message;
            var errorMsg = inner != null ? $"{ex.Message} | Inner: {inner}" : ex.Message;
            return StatusCode(500, new { reply = $"Chat error: {errorMsg}", error = errorMsg });
        }
    }

    // Send a voice note (recorded on device, uploaded on Send) — Gemini transcribes it
    [HttpPost("voice")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> SendVoiceMessage(
        [FromForm] IFormFile audio,
        [FromForm] Guid memberProfileId,
        [FromForm] Guid? conversationId)
    {
        if (audio is null || audio.Length == 0)
            return BadRequest("No audio file provided.");

        await using var stream = audio.OpenReadStream();
        var result = await _chatService.SendVoiceMessageAsync(
            memberProfileId,
            conversationId,
            stream,
            audio.ContentType);

        return Ok(result);
    }

    // Get all conversations
    [HttpGet("conversations/{memberProfileId}")]
    public async Task<IActionResult> GetConversations(Guid memberProfileId)
    {
        var result = await _chatService.GetConversationsAsync(memberProfileId);
        return Ok(result);
    }

    // Get messages in conversation
    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(Guid conversationId)
    {
        var result = await _chatService.GetConversationMessagesAsync(conversationId);
        return Ok(result);
    }

    // Create new conversation
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationDto dto)
    {
        var result = await _chatService.CreateConversationAsync(dto);
        return Ok(result);
    }

    // Delete conversation
    [HttpDelete("conversations/{conversationId}")]
    public async Task<IActionResult> DeleteConversation(Guid conversationId)
    {
        await _chatService.DeleteConversationAsync(conversationId);
        return NoContent();
    }
}
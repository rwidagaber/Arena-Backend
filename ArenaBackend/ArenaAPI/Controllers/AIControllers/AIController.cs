using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    // Send message (new or existing conversation)
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        var result = await _chatService.SendMessageAsync(
            dto.MemberProfileId,
            dto.ConversationId,
            dto.Message);
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
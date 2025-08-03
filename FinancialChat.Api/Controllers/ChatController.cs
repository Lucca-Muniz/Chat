using FinancialChat.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialChat.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatMessageService _chatService;

    public ChatController(IChatMessageService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("messages/{chatRoomId}")]
    public async Task<IActionResult> GetMessages(int chatRoomId)
    {
        var messages = await _chatService.GetRecentMessagesAsync(chatRoomId);
        return Ok(messages);
    }
}
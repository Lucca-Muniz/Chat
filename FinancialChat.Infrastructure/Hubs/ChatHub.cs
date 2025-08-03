using FinancialChat.Core.Interfaces;
using FinancialChat.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace FinancialChat.Infrastructure.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatMessageService _chatService;
    private readonly IStockCommandPublisher _stockPublisher;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IChatMessageService chatService,
        IStockCommandPublisher stockPublisher,
        ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _stockPublisher = stockPublisher;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var username = Context.User?.Identity?.Name ?? "Anonymous";
        var connectionId = Context.ConnectionId;

        _logger.LogInformation($"User {username} connected with ID: {connectionId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var username = Context.User?.Identity?.Name ?? "Anonymous";
        var connectionId = Context.ConnectionId;

        if (exception != null)
        {
            _logger.LogError(exception, $"User {username} disconnected with error: {exception.Message}");
        }
        else
        {
            _logger.LogInformation($"User {username} disconnected normally");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChatRoom(int chatRoomId)
    {
        try
        {
            var username = Context.User?.Identity?.Name ?? "Anonymous";
            _logger.LogInformation($"User {username} attempting to join chat room {chatRoomId}");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
            _logger.LogInformation($"User {username} successfully added to group ChatRoom_{chatRoomId}");

            var recentMessages = await _chatService.GetRecentMessagesAsync(chatRoomId);
            _logger.LogInformation($"Sending {recentMessages.Count()} recent messages to {username}");

            await Clients.Caller.SendAsync("LoadRecentMessages", recentMessages);
            _logger.LogInformation($"Recent messages sent successfully to {username}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in JoinChatRoom for room {chatRoomId}: {ex.Message}");
            throw;
        }
    }

    public async Task SendMessage(string message, int chatRoomId = 1)
    {
        try
        {
            var username = Context.User?.Identity?.Name ?? "Anonymous";
            _logger.LogInformation($"User {username} sending message to room {chatRoomId}: {message}");

            var stockCommandPattern = @"^/stock=([a-zA-Z0-9]+\.?[a-zA-Z]*)$";
            var match = Regex.Match(message.Trim(), stockCommandPattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var stockCode = match.Groups[1].Value.ToLower();
                _logger.LogInformation($"Processing stock command: {stockCode} for user {username}");

                await _stockPublisher.PublishStockCommandAsync(stockCode, username, chatRoomId);
                _logger.LogInformation($"Stock command published successfully");
                return;
            }

            var chatMessage = new ChatMessage
            {
                Content = message,
                Username = username,
                ChatRoomId = chatRoomId,
                Timestamp = DateTime.UtcNow
            };

            await _chatService.AddMessageAsync(chatMessage);
            _logger.LogInformation($"Message saved to database");

            await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("ReceiveMessage", chatMessage);
            _logger.LogInformation($"Message sent to group ChatRoom_{chatRoomId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in SendMessage: {ex.Message}");
            throw;
        }
    }

    public async Task LeaveChatRoom(int chatRoomId)
    {
        try
        {
            var username = Context.User?.Identity?.Name ?? "Anonymous";
            _logger.LogInformation($"User {username} leaving chat room {chatRoomId}");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
            _logger.LogInformation($"User {username} successfully removed from group ChatRoom_{chatRoomId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in LeaveChatRoom for room {chatRoomId}: {ex.Message}");
            throw;
        }
    }
}
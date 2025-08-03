using FinancialChat.Core.Interfaces;
using FinancialChat.Core.Models;
using FinancialChat.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialChat.Infrastructure.Services;

public class ChatMessageService : IChatMessageService
{
    private readonly ChatDbContext _context;

    public ChatMessageService(ChatDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(int chatRoomId, int count = 50)
    {
        return await _context.ChatMessages
            .Where(m => m.ChatRoomId == chatRoomId)
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
    }
}
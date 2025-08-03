using FinancialChat.Core.Models;

namespace FinancialChat.Core.Interfaces;

public interface IChatMessageService
{
    Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(int chatRoomId, int count = 50);
    Task AddMessageAsync(ChatMessage message);
}
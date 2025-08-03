namespace FinancialChat.Core.Interfaces;

public interface IStockCommandPublisher
{
    Task PublishStockCommandAsync(string stockCode, string username, int chatRoomId);
}
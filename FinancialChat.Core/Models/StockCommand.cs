namespace FinancialChat.Core.Models;

public class StockCommand
{
    public string StockCode { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int ChatRoomId { get; set; }
}

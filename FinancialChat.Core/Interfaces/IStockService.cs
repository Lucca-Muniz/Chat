using FinancialChat.Core.Models;

namespace FinancialChat.Core.Interfaces;

public interface IStockService
{
    Task<StockQuote?> GetStockQuoteAsync(string stockCode);
}
using FinancialChat.Core.Interfaces;
using FinancialChat.Core.Models;
using System.Globalization;

namespace FinancialChat.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly HttpClient _httpClient;

    public StockService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<StockQuote?> GetStockQuoteAsync(string stockCode)
    {
        try
        {
            var url = $"https://stooq.com/q/l/?s={stockCode}&f=sd2t2ohlcv&h&e=csv";
            var response = await _httpClient.GetStringAsync(url);

            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return null;

            var dataLine = lines[1].Split(',');
            if (dataLine.Length < 8) return null;

            return new StockQuote
            {
                Symbol = dataLine[0],
                Date = DateTime.ParseExact(dataLine[1], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Time = TimeOnly.ParseExact(dataLine[2], "HH:mm:ss", CultureInfo.InvariantCulture),
                Open = decimal.Parse(dataLine[3], CultureInfo.InvariantCulture),
                High = decimal.Parse(dataLine[4], CultureInfo.InvariantCulture),
                Low = decimal.Parse(dataLine[5], CultureInfo.InvariantCulture),
                Close = decimal.Parse(dataLine[6], CultureInfo.InvariantCulture),
                Volume = long.Parse(dataLine[7], CultureInfo.InvariantCulture)
            };
        }
        catch
        {
            return null;
        }
    }
}
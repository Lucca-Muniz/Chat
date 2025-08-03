using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialChat.Core.Models;

public class StockQuote
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Close { get; set; }
    public DateTime Date { get; set; }
    public TimeOnly Time { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public long Volume { get; set; }
}

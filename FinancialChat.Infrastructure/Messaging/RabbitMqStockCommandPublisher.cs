using FinancialChat.Core.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FinancialChat.Infrastructure.Messaging;

public class RabbitMqStockCommandPublisher : IStockCommandPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string QueueName = "stock_commands";

    public RabbitMqStockCommandPublisher(IConnection connection)
    {
        _connection = connection;
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(queue: QueueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
    }

    public async Task PublishStockCommandAsync(string stockCode, string username, int chatRoomId)
    {
        var command = new { StockCode = stockCode, Username = username, ChatRoomId = chatRoomId };
        var message = JsonSerializer.Serialize(command);
        var body = Encoding.UTF8.GetBytes(message);

        await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: QueueName, body: body);
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}
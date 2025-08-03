using FinancialChat.Core.Interfaces;
using FinancialChat.Core.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using FinancialChat.Infrastructure.Hubs;

namespace FinancialChat.Infrastructure.Messaging;

public class StockResponseConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockResponseConsumer> _logger;
    private readonly IHubContext<ChatHub> _hubContext;
    private const string ResponseQueueName = "stock_responses";

    public StockResponseConsumer(
        IConnection connection,
        IServiceScopeFactory scopeFactory,
        IHubContext<ChatHub> hubContext,
        ILogger<StockResponseConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(queue: ResponseQueueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var response = JsonSerializer.Deserialize<StockResponse>(message);

                if (response != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var chatService = scope.ServiceProvider.GetRequiredService<IChatMessageService>();

                    var chatMessage = new ChatMessage
                    {
                        Content = response.Message,
                        Username = "StockBot",
                        ChatRoomId = response.ChatRoomId,
                        IsBot = true,
                        Timestamp = DateTime.UtcNow
                    };

                    await chatService.AddMessageAsync(chatMessage);
                    await _hubContext.Clients.Group($"ChatRoom_{response.ChatRoomId}").SendAsync("ReceiveMessage", chatMessage, stoppingToken);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stock response");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(queue: ResponseQueueName, autoAck: false, consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}

public class StockResponse
{
    public string Message { get; set; } = string.Empty;
    public int ChatRoomId { get; set; }
}
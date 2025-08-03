using FinancialChat.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

public class StockBotService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IStockService _stockService;
    private readonly ILogger<StockBotService> _logger;
    private const string CommandQueueName = "stock_commands";
    private const string ResponseQueueName = "stock_responses";

    public StockBotService(IConnection connection, IStockService stockService, ILogger<StockBotService> logger)
    {
        _connection = connection;
        _stockService = stockService;
        _logger = logger;
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        // Declare queues
        _channel.QueueDeclareAsync(queue: CommandQueueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(queue: ResponseQueueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock Bot Service started");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var command = JsonSerializer.Deserialize<StockCommand>(message);

                if (command != null)
                {
                    _logger.LogInformation($"Processing stock command for {command.StockCode}");
                    await ProcessStockCommand(command);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stock command");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(queue: CommandQueueName, autoAck: false, consumer: consumer);

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessStockCommand(StockCommand command)
    {
        try
        {
            var quote = await _stockService.GetStockQuoteAsync(command.StockCode);
            string responseMessage;

            if (quote != null && quote.Close > 0)
            {
                responseMessage = $"{quote.Symbol.ToUpper()} quote is ${quote.Close:F2} per share";
            }
            else
            {
                responseMessage = $"Unable to retrieve quote for {command.StockCode.ToUpper()}. Please check the stock code and try again.";
            }

            var response = new StockResponse
            {
                Message = responseMessage,
                ChatRoomId = command.ChatRoomId
            };

            await PublishResponse(response);
            _logger.LogInformation($"Published response: {responseMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting stock quote for {command.StockCode}");

            var errorResponse = new StockResponse
            {
                Message = $"An error occurred while retrieving the stock quote for {command.StockCode.ToUpper()}. Please try again later.",
                ChatRoomId = command.ChatRoomId
            };

            await PublishResponse(errorResponse);
        }
    }

    private async Task PublishResponse(StockResponse response)
    {
        var message = JsonSerializer.Serialize(response);
        var body = Encoding.UTF8.GetBytes(message);

        await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: ResponseQueueName, body: body);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}

public class StockCommand
{
    public string StockCode { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int ChatRoomId { get; set; }
}

public class StockResponse
{
    public string Message { get; set; } = string.Empty;
    public int ChatRoomId { get; set; }
}
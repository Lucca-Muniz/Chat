using FinancialChat.Core.Interfaces;
using FinancialChat.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

// Configure services
builder.Services.AddHttpClient<IStockService, StockService>();
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest"
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddHostedService<StockBotService>();

var host = builder.Build();
await host.RunAsync();

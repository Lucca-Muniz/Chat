using FinancialChat.Core.Interfaces;
using FinancialChat.Core.Models;
using FinancialChat.Infrastructure.Data;
using FinancialChat.Infrastructure.Hubs;
using FinancialChat.Infrastructure.Messaging;
using FinancialChat.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Enable detailed errors for debugging
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Database
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlite("Data Source=chat.db"));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<ChatDbContext>()
.AddDefaultTokenProviders();

// JWT settings
var jwtSettings = new JwtSettings
{
    SecretKey = "MyVeryLongSecretKeyThatShouldBeAtLeast32Characters!",
    Issuer = "FinancialChatApp",
    Audience = "FinancialChatUsers",
    ExpirationMinutes = 60
};

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IJwtService, JwtService>();

// JWT Auth
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validated for user: {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        }
    };
});

// RabbitMQ 
builder.Services.AddSingleton<IConnection>(sp =>
{
    try
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };
        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = sp.GetService<ILogger<Program>>();
        logger?.LogWarning(ex, "Failed to connect to RabbitMQ. Stock commands will not work.");
        return null;
    }
});

// Services
builder.Services.AddScoped<IChatMessageService, ChatMessageService>();
builder.Services.AddScoped<IStockCommandPublisher, RabbitMqStockCommandPublisher>();
builder.Services.AddHttpClient<IStockService, StockService>();
builder.Services.AddHostedService<StockResponseConsumer>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFile", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.UseCors("LocalFile");

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();
app.MapHub<ChatHub>("/chathub");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        context.Database.EnsureCreated();
        logger.LogInformation("Database created/verified successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating database");
        throw;
    }
}

app.Run();

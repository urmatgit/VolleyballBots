using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http;
using Telegram.Bot;
using VolleyballBot.Bot.Handlers;
using VolleyballBot.Bot.Services;
using VolleyballBot.Infrastructure.Database;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Конфигурация
        builder.Services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(30);
        });

        // БД
        var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"] ?? "Data Source=volleyball.db";
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // Репозитории
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IGameRepository, GameRepository>();

        // Сервисы
        builder.Services.AddScoped<IGameService, GameService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddSingleton<IUserStateService, UserStateService>();
        builder.Services.AddSingleton<IKeyboardService, KeyboardService>();

        // Handlers
        builder.Services.AddScoped<MenuHandler>();
        builder.Services.AddScoped<CreateGameHandler>();
        builder.Services.AddScoped<GamesHandler>();
        builder.Services.AddScoped<CallbackHandler>();
        builder.Services.AddScoped<AdminGamesHandler>();

        // Telegram Bot
        var botToken = builder.Configuration["BotToken"];
        if (string.IsNullOrEmpty(botToken))
        {
            throw new InvalidOperationException("BotToken не настроен");
        }

        var httpClient = CreateHttpClient(builder.Configuration);
        builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken, httpClient));

        // Hosted Service
        builder.Services.AddHostedService<TelegramBotHostedService>();

        // Build app
        var app = builder.Build();

        // Health check для Render
        HealthCheck.Configure(app);

        // Initialize database
        using (var scope = app.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        // Run
        await app.RunAsync();
    }

    private static HttpClient CreateHttpClient(IConfiguration configuration)
    {
        var httpClient = new HttpClient();
        var proxyUrl = configuration["Proxy:Url"];
        
        if (!string.IsNullOrEmpty(proxyUrl))
        {
            var proxy = new WebProxy(proxyUrl) { UseDefaultCredentials = true };
            var proxyUser = configuration["Proxy:Username"];
            var proxyPass = configuration["Proxy:Password"];
            
            if (!string.IsNullOrEmpty(proxyUser))
            {
                proxy.Credentials = new NetworkCredential(proxyUser, proxyPass ?? "");
            }
            
            httpClient = new HttpClient(new HttpClientHandler { Proxy = proxy, UseProxy = true });
        }

        return httpClient;
    }
}

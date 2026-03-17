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
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Конфигурация
                services.Configure<HostOptions>(options =>
                {
                    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
                });

                // БД
                var connectionString = context.Configuration["ConnectionStrings:DefaultConnection"] ?? "Data Source=volleyball.db";
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(connectionString));

                // Репозитории
                services.AddScoped<IUserRepository, UserRepository>();
                services.AddScoped<IGameRepository, GameRepository>();

                // Сервисы
                services.AddScoped<IGameService, GameService>();
                services.AddScoped<INotificationService, NotificationService>();
                services.AddSingleton<IUserStateService, UserStateService>();
                services.AddSingleton<IKeyboardService, KeyboardService>();

                // Handlers
                services.AddScoped<MenuHandler>();
                services.AddScoped<CreateGameHandler>();
                services.AddScoped<GamesHandler>();
                services.AddScoped<CallbackHandler>();
                services.AddScoped<AdminGamesHandler>();

                // Telegram Bot
                var botToken = context.Configuration["BotToken"];
                if (string.IsNullOrEmpty(botToken))
                {
                    throw new InvalidOperationException("BotToken не настроен в appsettings.json");
                }

                // Настройка прокси для разработки
                var httpClient = new HttpClient();
                var proxyUrl = context.Configuration["Proxy:Url"];
                if (!string.IsNullOrEmpty(proxyUrl))
                {
                    var proxy = new WebProxy(proxyUrl)
                    {
                        UseDefaultCredentials = true
                    };
                    var proxyUser = context.Configuration["Proxy:Username"];
                    var proxyPass = context.Configuration["Proxy:Password"];
                    if (!string.IsNullOrEmpty(proxyUser))
                    {
                        proxy.Credentials = new NetworkCredential(proxyUser, proxyPass ?? "");
                    }
                    httpClient = new HttpClient(new HttpClientHandler { Proxy = proxy, UseProxy = true });
                }

                services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken, httpClient));

                // Hosted Service
                services.AddHostedService<TelegramBotHostedService>();
            })
            .Build();

        // Применяем миграции БД
        using (var scope = host.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        await host.RunAsync();
    }
}

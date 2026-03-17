using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VolleyballBot.Bot.Handlers;
using VolleyballBot.Bot.Services;
using VolleyballBot.Services;

namespace VolleyballBot;

public class TelegramBotHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramBotHostedService> _logger;

    public TelegramBotHostedService(
        IServiceProvider serviceProvider,
        ITelegramBotClient botClient,
        ILogger<TelegramBotHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _botClient = botClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Запуск Telegram бота...");

        // Получаем информацию о боте
        var bot = await _botClient.GetMe(stoppingToken);
        _logger.LogInformation($"Бот запущен: @{bot.Username}");

        // Настраиваем команды меню
        await SetBotCommands(stoppingToken);

        // Запускаем получение обновлений
        await ReceiveUpdates(stoppingToken);
    }

    private async Task SetBotCommands(CancellationToken cancellationToken)
    {
        var commands = new[]
        {
            new Telegram.Bot.Types.BotCommand("start", "Запустить бота"),
            new Telegram.Bot.Types.BotCommand("help", "Справка"),
        };

        await _botClient.SetMyCommands(commands, cancellationToken: cancellationToken);
        _logger.LogInformation("Команды бота установлены");
    }

    private async Task ReceiveUpdates(CancellationToken stoppingToken)
    {
        long? offset = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await _botClient.GetUpdates(offset: offset.HasValue ? (int?)offset.Value : null, timeout: 30, cancellationToken: stoppingToken);

                foreach (var update in updates)
                {
                    offset = update.Id + 1;
                    await HandleUpdateAsync(update, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Нормальное завершение
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении обновлений");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task HandleUpdateAsync(Update update, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateAsyncScope();

        try
        {
            if (update.Type == UpdateType.Message)
            {
                await HandleMessageAsync(scope.ServiceProvider, update, stoppingToken);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                var callbackHandler = scope.ServiceProvider.GetRequiredService<CallbackHandler>();
                await callbackHandler.HandleAsync(_botClient, update);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке обновления");
        }
    }

    private async Task HandleMessageAsync(IServiceProvider serviceProvider, Update update, CancellationToken stoppingToken)
    {
        var message = update.Message;
        if (message?.Text == null) return;

        var text = message.Text.Trim();
        var telegramId = message.From.Id;

        // Получаем сервисы
        var menuHandler = serviceProvider.GetRequiredService<MenuHandler>();
        var createGameHandler = serviceProvider.GetRequiredService<CreateGameHandler>();
        var adminGamesHandler = serviceProvider.GetRequiredService<AdminGamesHandler>();
        var userStateService = serviceProvider.GetRequiredService<IUserStateService>();

        // Проверяем состояние пользователя (пошаговое создание игры)
        var state = userStateService.GetState(telegramId);
        if (state != UserState.None)
        {
            if (text.ToLower() == "/cancel")
            {
                await createGameHandler.HandleCancelAsync(_botClient, update);
            }
            else
            {
                await createGameHandler.HandleInputAsync(_botClient, update, text);
            }
            return;
        }

        // Обработка кнопок меню и команд
        switch (text.ToLower())
        {
            case "/start":
                await menuHandler.HandleStartAsync(_botClient, update);
                break;

            case "/help":
            case "ℹ️ помощь":
            case "помощь":
                await menuHandler.HandleHelpAsync(_botClient, update);
                break;

            case "🏐 доступные игры":
            case "доступные игры":
                await menuHandler.HandleAvailableGamesAsync(_botClient, update);
                break;

            case "📋 мои записи":
            case "мои записи":
                await menuHandler.HandleMyGamesAsync(_botClient, update);
                break;

            case "➕ создать игру":
            case "создать игру":
                await createGameHandler.HandleStartCreateGameAsync(_botClient, update);
                break;

            case "📊 мои игры":
            case "мои игры":
                await adminGamesHandler.HandleMyGamesAsync(_botClient, update);
                break;

            default:
                // Неизвестная команда - показываем меню
                await menuHandler.HandleHelpAsync(_botClient, update);
                break;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Остановка Telegram бота...");
        await base.StopAsync(cancellationToken);
    }
}

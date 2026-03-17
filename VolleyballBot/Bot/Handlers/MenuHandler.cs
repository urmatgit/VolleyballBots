using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using VolleyballBot.Bot.Services;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot.Bot.Handlers;

public class MenuHandler
{
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IKeyboardService _keyboardService;
    private readonly IGameService _gameService;
    private readonly IConfiguration _configuration;

    public MenuHandler(
        INotificationService notificationService,
        IUserRepository userRepository,
        IKeyboardService keyboardService,
        IGameService gameService,
        IConfiguration configuration)
    {
        _notificationService = notificationService;
        _userRepository = userRepository;
        _keyboardService = keyboardService;
        _gameService = gameService;
        _configuration = configuration;
    }

    public async Task HandleStartAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;
        var username = update.Message.From.Username;
        var firstName = update.Message.From.FirstName;

        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null)
        {
            await _userRepository.CreateAsync(telegramId, username);
        }

        var isAdmin = IsAdmin(telegramId);
        var keyboard = isAdmin ? _keyboardService.GetAdminMenu() : _keyboardService.GetMainMenu();

        var message = $"""
            🏐 *Добро пожаловать, {firstName}!*
            
            Я помогу организовать волейбольную игру.
            
            {(isAdmin ? "Вы администратор — вам доступно создание игр." : "")}
            
            Выберите действие в меню:
            """;

        await botClient.SendMessage(telegramId, message, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
    }

    public async Task HandleHelpAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;

        var message = """
            ℹ️ *Справка*
            
            *Как записаться на игру:*
            1. Нажмите "🏐 Доступные игры"
            2. Выберите игру из списка
            3. Нажмите "🟢 Записаться"
            
            *Как отписаться:*
            - Откройте игру в "📋 Мои записи"
            - Нажмите "Отписаться"
            - ⚠️ Отписаться можно не позднее чем за 3 часа до начала
            
            *Статусы записи:*
            🟢 Игрок — вы в основном составе
            🟡 Резерв — вы в списке ожидания
            
            *Для админов:*
            - "➕ Создать игру" — пошаговое создание
            - "📊 Мои игры" — управление созданными играми
            """;

        await _notificationService.SendMessageAsync(telegramId, message);
    }

    public async Task HandleAvailableGamesAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;

        var games = await _gameService.GetAvailableGamesAsync();

        if (games.Count == 0)
        {
            await _notificationService.SendMessageAsync(telegramId, "📭 Сейчас нет доступных игр.\n\nСледите за обновлениями!");
            return;
        }

        var keyboard = _keyboardService.GetGamesList(games);

        var message = "🏐 *Доступные игры:*\n\n";

        foreach (var game in games)
        {
            var playerCount = game.GamePlayers.Count(gp => gp.Status == Domain.Enums.PlayerStatus.Player);
            var reserveCount = game.GamePlayers.Count(gp => gp.Status == Domain.Enums.PlayerStatus.Reserve);
            var status = playerCount >= game.MaxPlayers ? "🟡 Резерв" : "🟢 Есть места";

            message += $"""
                *#{game.Id}* - {game.DateTime:dd.MM.yyyy} в {game.DateTime:HH:mm}
                📍 {game.Address}
                👥 {playerCount}/{game.MaxPlayers} игроков ({reserveCount} в резерве)
                💰 {game.Price} ₽
                {status}
                
                """;
        }

        await botClient.SendMessage(telegramId, message, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
    }

    public async Task HandleMyGamesAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;
        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null)
        {
            await _notificationService.SendMessageAsync(telegramId, "Сначала нажмите /start для регистрации");
            return;
        }

        var allGames = await _gameService.GetAvailableGamesAsync();
        var myGames = allGames.Where(g => g.GamePlayers.Any(gp => gp.UserId == user.Id)).ToList();

        if (myGames.Count == 0)
        {
            await _notificationService.SendMessageAsync(telegramId, "📭 Вы пока не записаны ни на одну игру.\n\nИспользуйте \"🏐 Доступные игры\" для записи.");
            return;
        }

        var keyboard = _keyboardService.GetMyGamesList(myGames);

        var message = "📋 *Ваши записи:*\n\n";

        foreach (var game in myGames)
        {
            var myStatus = game.GamePlayers.FirstOrDefault(gp => gp.UserId == user.Id)?.Status;
            var statusIcon = myStatus == Domain.Enums.PlayerStatus.Reserve ? "🟡" : "🟢";
            var statusText = myStatus == Domain.Enums.PlayerStatus.Reserve ? "(резерв)" : "(игрок)";
            var playerCount = game.GamePlayers.Count(gp => gp.Status == Domain.Enums.PlayerStatus.Player);

            message += $"""
                {statusIcon} *#{game.Id}* - {game.DateTime:dd.MM.yyyy} в {game.DateTime:HH:mm} {statusText}
                📍 {game.Address}
                👥 {playerCount}/{game.MaxPlayers} игроков
                💰 {game.Price} ₽
                
                """;
        }

        await botClient.SendMessage(telegramId, message, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
    }

    private bool IsAdmin(long telegramId)
    {
        var adminIds = _configuration.GetSection("AdminTelegramIds").Get<List<long>>() ?? new List<long>();
        return adminIds.Contains(telegramId);
    }
}

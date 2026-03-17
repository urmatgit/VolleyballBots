using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using VolleyballBot.Bot.Services;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot.Bot.Handlers;

/// <summary>
/// Обработчик управления играми для админа
/// </summary>
public class AdminGamesHandler
{
    private readonly IGameService _gameService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IKeyboardService _keyboardService;
    private readonly IConfiguration _configuration;

    public AdminGamesHandler(
        IGameService gameService,
        INotificationService notificationService,
        IUserRepository userRepository,
        IGameRepository gameRepository,
        IKeyboardService keyboardService,
        IConfiguration configuration)
    {
        _gameService = gameService;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _gameRepository = gameRepository;
        _keyboardService = keyboardService;
        _configuration = configuration;
    }

    /// <summary>
    /// Показать список игр админа с кнопками управления
    /// </summary>
    public async Task HandleMyGamesAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;
        
        if (!IsAdmin(telegramId))
        {
            await _notificationService.SendMessageAsync(telegramId, "❌ У вас нет прав администратора");
            return;
        }

        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null)
        {
            await _notificationService.SendMessageAsync(telegramId, "❌ Пользователь не найден");
            return;
        }

        var allGames = await _gameService.GetAvailableGamesAsync();
        var myGames = allGames.Where(g => g.CreatedBy == user.Id).OrderByDescending(g => g.DateTime).ToList();

        if (myGames.Count == 0)
        {
            await _notificationService.SendMessageAsync(telegramId, "📭 У вас пока нет созданных игр");
            return;
        }

        var message = "📊 *Ваши игры:*\n\n";

        foreach (var game in myGames)
        {
            var playerCount = game.GamePlayers.Count(gp => gp.Status == Domain.Enums.PlayerStatus.Player);
            var status = game.IsActive ? "🟢 Активна" : "❌ Отменена";
            
            message += $"""
                *#{game.Id}* - {game.DateTime:dd.MM.yyyy} в {game.DateTime:HH:mm}
                📍 {game.Address}
                👥 {playerCount}/{game.MaxPlayers} игроков
                💰 {game.Price} ₽
                {status}
                
                """;
        }

        message += "\nВыберите игру для управления:";

        var keyboard = GetAdminGamesKeyboard(myGames);

        await botClient.SendMessage(telegramId, message, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
    }

    /// <summary>
    /// Показать детали игры с кнопками управления
    /// </summary>
    public async Task HandleGameDetailsAsync(ITelegramBotClient botClient, long telegramId, long gameId)
    {
        if (!IsAdmin(telegramId))
        {
            await botClient.SendMessage(telegramId, "❌ У вас нет прав администратора");
            return;
        }

        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null) return;

        var games = await _gameService.GetAvailableGamesAsync();
        var game = games.FirstOrDefault(g => g.Id == gameId);

        if (game == null)
        {
            await botClient.SendMessage(telegramId, "❌ Игра не найдена");
            return;
        }

        if (game.CreatedBy != user.Id)
        {
            await botClient.SendMessage(telegramId, "❌ Это не ваша игра");
            return;
        }

        var playerCount = game.GamePlayers.Count(gp => gp.Status == Domain.Enums.PlayerStatus.Player);
        var reserveCount = game.GamePlayers.Count(gp => gp.Status == Domain.Enums.PlayerStatus.Reserve);

        var message = $"""
            🏐 *Игра #{game.Id}*
            
            📅 {game.DateTime:dd.MM.yyyy} ({game.DateTime:dddd})
            ⏰ {game.DateTime:HH:mm}
            📍 {game.Address}
            
            👥 Игроки: {playerCount}/{game.MaxPlayers}
            🟡 Резерв: {reserveCount}
            💰 Цена: {game.Price} ₽
            {(game.IsActive ? "🟢 Активна" : "❌ Отменена")}
            
            *Список игроков:*
            """;

        var players = game.GamePlayers.Where(gp => gp.Status == Domain.Enums.PlayerStatus.Player).ToList();
        var reserves = game.GamePlayers.Where(gp => gp.Status == Domain.Enums.PlayerStatus.Reserve).ToList();

        if (players.Count == 0)
        {
            message += "\n_Пока нет записанных игроков_";
        }
        else
        {
            foreach (var p in players)
            {
                message += $"\n• {p.User?.Username ?? "Игрок"}";
            }
        }

        if (reserves.Count > 0)
        {
            message += "\n\n*Резерв:*";
            foreach (var r in reserves)
            {
                message += $"\n• {r.User?.Username ?? "Игрок"}";
            }
        }

        var keyboard = GetAdminGameControlKeyboard(gameId, game.IsActive);

        await botClient.SendMessage(telegramId, message, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
    }

    /// <summary>
    /// Удалить игру
    /// </summary>
    public async Task HandleDeleteGameAsync(ITelegramBotClient botClient, long telegramId, long gameId)
    {
        if (!IsAdmin(telegramId))
        {
            await botClient.SendMessage(telegramId, "❌ У вас нет прав администратора");
            return;
        }

        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null) return;

        var game = await _gameService.GetAvailableGamesAsync();
        var selectedGame = game.FirstOrDefault(g => g.Id == gameId);

        if (selectedGame == null)
        {
            await botClient.SendMessage(telegramId, "❌ Игра не найдена");
            return;
        }

        if (selectedGame.CreatedBy != user.Id)
        {
            await botClient.SendMessage(telegramId, "❌ Это не ваша игра");
            return;
        }

        await _gameRepository.DeleteAsync(gameId);

        // Уведомить всех игроков об отмене игры
        var players = selectedGame.GamePlayers.Select(gp => gp.User).Where(u => u != null).ToList();
        await _notificationService.NotifyGameCancelledAsync(selectedGame, players!);

        await botClient.SendMessage(telegramId, $"✅ Игра #{gameId} удалена. Все игроки уведомлены.");
    }

    /// <summary>
    /// Отменить игру
    /// </summary>
    public async Task HandleCancelGameAsync(ITelegramBotClient botClient, long telegramId, long gameId)
    {
        if (!IsAdmin(telegramId))
        {
            await botClient.SendMessage(telegramId, "❌ У вас нет прав администратора");
            return;
        }

        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null) return;

        var game = await _gameService.CancelGameAsync(gameId);

        if (game == null)
        {
            await botClient.SendMessage(telegramId, "❌ Игра не найдена или уже отменена");
            return;
        }

        if (game.CreatedBy != user.Id)
        {
            await botClient.SendMessage(telegramId, "❌ Это не ваша игра");
            return;
        }

        // Уведомить всех игроков
        var players = game.GamePlayers.Select(gp => gp.User).Where(u => u != null).ToList();
        await _notificationService.NotifyGameCancelledAsync(game, players!);

        await botClient.SendMessage(telegramId, $"✅ Игра #{gameId} отменена. Все игроки уведомлены.");
    }

    /// <summary>
    /// Обработка callback от кнопок админа
    /// </summary>
    public async Task HandleCallbackAsync(ITelegramBotClient botClient, Update update, string data)
    {
        if (update.CallbackQuery == null) return;

        var telegramId = update.CallbackQuery.From.Id;

        if (data.StartsWith("admin_game_details_"))
        {
            var gameId = long.Parse(data.Substring("admin_game_details_".Length));
            await HandleGameDetailsAsync(botClient, telegramId, gameId);
        }
        else if (data.StartsWith("admin_delete_game_"))
        {
            var gameId = long.Parse(data.Substring("admin_delete_game_".Length));
            
            // Показываем подтверждение
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, "Вы уверены? Это действие нельзя отменить.", showAlert: true);
            
            await HandleDeleteGameAsync(botClient, telegramId, gameId);
            
            // Удаляем сообщение со списком
            await botClient.DeleteMessage(telegramId, update.CallbackQuery.Message.MessageId);
        }
        else if (data.StartsWith("admin_cancel_game_"))
        {
            var gameId = long.Parse(data.Substring("admin_cancel_game_".Length));
            await HandleCancelGameAsync(botClient, telegramId, gameId);
        }
        else if (data == "admin_games_back")
        {
            await botClient.DeleteMessage(telegramId, update.CallbackQuery.Message.MessageId);
            await HandleMyGamesAsync(botClient, update);
        }

        await botClient.AnswerCallbackQuery(update.CallbackQuery.Id);
    }

    private InlineKeyboardMarkup GetAdminGamesKeyboard(List<Domain.Entities.Game> games)
    {
        var buttons = games.Select(g => new[]
        {
            InlineKeyboardButton.WithCallbackData(
                $"📅 {g.DateTime:dd.MM} {g.DateTime:HH:mm} | {(g.IsActive ? "🟢" : "❌")}",
                $"admin_game_details_{g.Id}")
        }).ToArray();

        buttons = buttons.Concat(new[] { 
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад в меню", "main_menu") }
        }).ToArray();

        return new InlineKeyboardMarkup(buttons);
    }

    private InlineKeyboardMarkup GetAdminGameControlKeyboard(long gameId, bool isActive)
    {
        var buttons = new List<InlineKeyboardButton>();

        if (isActive)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("❌ Отменить игру", $"admin_cancel_game_{gameId}"));
            buttons.Add(InlineKeyboardButton.WithCallbackData("🗑 Удалить игру", $"admin_delete_game_{gameId}"));
        }

        buttons.Add(InlineKeyboardButton.WithCallbackData("🔙 Назад к списку", "admin_games_back"));

        return new InlineKeyboardMarkup(buttons.ToArray());
    }

    private bool IsAdmin(long telegramId)
    {
        var adminIds = _configuration.GetSection("AdminTelegramIds").Get<List<long>>() ?? new List<long>();
        return adminIds.Contains(telegramId);
    }
}

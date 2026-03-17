using Telegram.Bot;
using Telegram.Bot.Types;
using VolleyballBot.Bot.Services;
using VolleyballBot.Domain.Enums;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot.Bot.Handlers;

public class CallbackHandler
{
    private readonly IGameService _gameService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly GamesHandler _gamesHandler;
    private readonly IKeyboardService _keyboardService;

    public CallbackHandler(
        IGameService gameService,
        INotificationService notificationService,
        IUserRepository userRepository,
        GamesHandler gamesHandler,
        IKeyboardService keyboardService)
    {
        _gameService = gameService;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _gamesHandler = gamesHandler;
        _keyboardService = keyboardService;
    }

    public async Task HandleAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.CallbackQuery == null || string.IsNullOrEmpty(update.CallbackQuery.Data)) return;

        var data = update.CallbackQuery.Data;
        var telegramId = update.CallbackQuery.From.Id;

        if (data.StartsWith("view_game_"))
        {
            var gameId = long.Parse(data.Substring("view_game_".Length));
            await _gamesHandler.HandleViewGameAsync(botClient, telegramId, gameId);
        }
        else if (data.StartsWith("join_game_"))
        {
            var gameId = long.Parse(data.Substring("join_game_".Length));
            await HandleJoinAsync(botClient, update, gameId);
        }
        else if (data.StartsWith("leave_game_"))
        {
            var gameId = long.Parse(data.Substring("leave_game_".Length));
            await HandleLeaveAsync(botClient, update, gameId);
        }
        else if (data == "games_list")
        {
            // Возврат к списку игр - закрываем текущее сообщение
            await botClient.DeleteMessage(telegramId, update.CallbackQuery.Message.MessageId);
        }
        else if (data == "main_menu")
        {
            // Возврат в главное меню
            await botClient.DeleteMessage(telegramId, update.CallbackQuery.Message.MessageId);
        }

        await botClient.AnswerCallbackQuery(update.CallbackQuery.Id);
    }

    private async Task HandleJoinAsync(ITelegramBotClient botClient, Update update, long gameId)
    {
        if (update.CallbackQuery == null) return;

        var telegramId = update.CallbackQuery.From.Id;
        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null)
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, "Сначала нажмите /start", showAlert: true);
            return;
        }

        var (success, message, status) = await _gameService.JoinGameAsync(gameId, user.Id);

        if (success)
        {
            var games = await _gameService.GetAvailableGamesAsync();
            var selectedGame = games.FirstOrDefault(g => g.Id == gameId);
            if (selectedGame != null)
            {
                await _notificationService.NotifyPlayerJoinedAsync(selectedGame, user, status);
            }
            // Обновляем сообщение со списком игроков
            await _gamesHandler.HandleViewGameAsync(botClient, telegramId, gameId);
        }

        await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, message, showAlert: !success);
    }

    private async Task HandleLeaveAsync(ITelegramBotClient botClient, Update update, long gameId)
    {
        if (update.CallbackQuery == null) return;

        var telegramId = update.CallbackQuery.From.Id;
        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null)
        {
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, "Сначала нажмите /start", showAlert: true);
            return;
        }

        var (success, message) = await _gameService.LeaveGameAsync(gameId, user.Id);

        if (success)
        {
            var games = await _gameService.GetAvailableGamesAsync();
            var selectedGame = games.FirstOrDefault(g => g.Id == gameId);
            if (selectedGame != null)
            {
                await _notificationService.NotifyPlayerLeftAsync(selectedGame, user);
            }
            // Обновляем сообщение со списком игроков
            await _gamesHandler.HandleViewGameAsync(botClient, telegramId, gameId);
        }

        await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, message, showAlert: !success);
    }
}

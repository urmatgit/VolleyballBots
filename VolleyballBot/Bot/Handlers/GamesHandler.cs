using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using VolleyballBot.Bot.Services;
using VolleyballBot.Domain.Entities;
using VolleyballBot.Domain.Enums;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot.Bot.Handlers;

public class GamesHandler
{
    private readonly IGameService _gameService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IKeyboardService _keyboardService;

    public GamesHandler(IGameService gameService, INotificationService notificationService, IUserRepository userRepository, IKeyboardService keyboardService)
    {
        _gameService = gameService;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _keyboardService = keyboardService;
    }

    public async Task HandleViewGameAsync(ITelegramBotClient botClient, long telegramId, long gameId)
    {
        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null)
        {
            await botClient.SendMessage(telegramId, "❌ Сначала нажмите /start");
            return;
        }

        var games = await _gameService.GetAvailableGamesAsync();
        var selectedGame = games.FirstOrDefault(g => g.Id == gameId);
        if (selectedGame == null)
        {
            await botClient.SendMessage(telegramId, "❌ Игра не найдена");
            return;
        }

        var playerCount = selectedGame.GamePlayers.Count(gp => gp.Status == PlayerStatus.Player);
        var isJoined = selectedGame.GamePlayers.Any(gp => gp.UserId == user.Id);
        var myStatus = selectedGame.GamePlayers.FirstOrDefault(gp => gp.UserId == user.Id)?.Status;

        var message = $"""
            🏐 *Игра #{selectedGame.Id}*
            
            📅 {selectedGame.DateTime:dd.MM.yyyy} ({selectedGame.DateTime:dddd})
            ⏰ {selectedGame.DateTime:HH:mm}
            📍 {selectedGame.Address}
            
            👥 Игроки: {playerCount}/{selectedGame.MaxPlayers}
            💰 Цена: {selectedGame.Price} ₽
            
            *Список игроков:*
            """;

        var players = selectedGame.GamePlayers.Where(gp => gp.Status == PlayerStatus.Player).ToList();
        var reserves = selectedGame.GamePlayers.Where(gp => gp.Status == PlayerStatus.Reserve).ToList();

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

        var keyboard = _keyboardService.GetGameDetails(gameId, selectedGame.Address, isJoined, myStatus);

        await botClient.SendMessage(telegramId, message, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
    }
}

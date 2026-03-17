using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VolleyballBot.Domain.Enums;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot.Bot.Handlers;

public class PlayersHandler
{
    private readonly IGameService _gameService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;

    public PlayersHandler(IGameService gameService, INotificationService notificationService, IUserRepository userRepository)
    {
        _gameService = gameService;
        _notificationService = notificationService;
        _userRepository = userRepository;
    }

    public async Task HandleAsync(ITelegramBotClient botClient, Update update, string? gameIdArg)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;
        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null)
        {
            await _notificationService.SendMessageAsync(telegramId, "Сначала нажмите /start для регистрации");
            return;
        }

        if (string.IsNullOrEmpty(gameIdArg) || !long.TryParse(gameIdArg, out var gameId))
        {
            await _notificationService.SendMessageAsync(telegramId, "❌ Неверный формат. Используйте: /players <id игры>");
            return;
        }

        var gamePlayers = await _gameService.GetGamePlayersAsync(gameId);
        if (gamePlayers.Count == 0)
        {
            await _notificationService.SendMessageAsync(telegramId, "❌ Игра не найдена или на неё никто не записан");
            return;
        }

        var game = await _gameService.GetAvailableGamesAsync();
        var selectedGame = game.FirstOrDefault(g => g.Id == gameId);

        var message = $"""
            🏐 *Игроки на игре #{gameId}*
            
            📅 {selectedGame?.DateTime:dd.MM.yyyy} в {selectedGame?.DateTime:HH:mm}
            📍 {selectedGame?.Address}
            
            *Основной состав ({gamePlayers.Count(gp => gp.Status == PlayerStatus.Player)}/{selectedGame?.MaxPlayers}):*
            """;

        var players = gamePlayers.Where(gp => gp.Status == PlayerStatus.Player).ToList();
        var reserves = gamePlayers.Where(gp => gp.Status == PlayerStatus.Reserve).ToList();

        if (players.Count == 0)
        {
            message += "\n_Пока нет записанных игроков_";
        }
        else
        {
            foreach (var p in players)
            {
                var joinedTime = p.JoinedAt.ToLocalTime().ToString("HH:mm");
                message += $"\n• {p.User?.Username ?? "Игрок"} (записался в {joinedTime})";
            }
        }

        if (reserves.Count > 0)
        {
            message += "\n\n*Резерв:*";
            foreach (var r in reserves)
            {
                var joinedTime = r.JoinedAt.ToLocalTime().ToString("HH:mm");
                message += $"\n• {r.User?.Username ?? "Игрок"} (записался в {joinedTime})";
            }
        }

        await _notificationService.SendMessageAsync(telegramId, message);
    }
}

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VolleyballBot.Domain.Enums;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot.Bot.Handlers;

public class JoinHandler
{
    private readonly IGameService _gameService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;

    public JoinHandler(IGameService gameService, INotificationService notificationService, IUserRepository userRepository)
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
            await _notificationService.SendMessageAsync(telegramId, "❌ Неверный формат. Используйте: /join <id игры>");
            return;
        }

        var (success, message, status) = await _gameService.JoinGameAsync(gameId, user.Id);

        if (success)
        {
            var game = await _gameService.GetAvailableGamesAsync();
            var selectedGame = game.FirstOrDefault(g => g.Id == gameId);
            if (selectedGame != null)
            {
                await _notificationService.NotifyPlayerJoinedAsync(selectedGame, user, status);
            }
        }

        var icon = status == PlayerStatus.Player ? "🟢" : "🟡";
        await _notificationService.SendMessageAsync(telegramId, $"{icon} {message}");
    }
}

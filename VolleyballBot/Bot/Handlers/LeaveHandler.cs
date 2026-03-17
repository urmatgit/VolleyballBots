using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot.Bot.Handlers;

public class LeaveHandler
{
    private readonly IGameService _gameService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;

    public LeaveHandler(IGameService gameService, INotificationService notificationService, IUserRepository userRepository)
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
            await _notificationService.SendMessageAsync(telegramId, "❌ Неверный формат. Используйте: /leave <id игры>");
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
        }

        var icon = success ? "✅" : "❌";
        await _notificationService.SendMessageAsync(telegramId, $"{icon} {message}");
    }
}

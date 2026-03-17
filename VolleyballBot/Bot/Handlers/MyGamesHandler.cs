using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VolleyballBot.Domain.Enums;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot.Bot.Handlers;

public class MyGamesHandler
{
    private readonly IGameService _gameService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;

    public MyGamesHandler(IGameService gameService, INotificationService notificationService, IUserRepository userRepository)
    {
        _gameService = gameService;
        _notificationService = notificationService;
        _userRepository = userRepository;
    }

    public async Task HandleAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;
        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null)
        {
            await _notificationService.SendMessageAsync(telegramId, "Сначала нажмите /start для регистрации");
            return;
        }

        var games = await _gameService.GetMyGamesAsync(user.Id);

        if (games.Count == 0)
        {
            await _notificationService.SendMessageAsync(telegramId, "📭 Вы пока не записаны ни на одну игру.\n\nИспользуйте /games для просмотра доступных игр.");
            return;
        }

        var message = "🏐 *Ваши записи:*\n\n";

        foreach (var game in games)
        {
            var myStatus = game.GamePlayers.FirstOrDefault(gp => gp.UserId == user.Id)?.Status;
            var statusIcon = myStatus == PlayerStatus.Reserve ? "🟡" : "🟢";
            var statusText = myStatus == PlayerStatus.Reserve ? "(резерв)" : "(игрок)";
            var playerCount = game.GamePlayers.Count(gp => gp.Status == PlayerStatus.Player);

            message += $"""
                {statusIcon} *#{game.Id}* - {game.DateTime:dd.MM.yyyy} в {game.DateTime:HH:mm} {statusText}
                📍 {game.Address}
                👥 {playerCount}/{game.MaxPlayers} игроков
                💰 {game.Price} ₽
                
                """;
        }

        message += "\nДля отписки используйте: /leave <id>";
        await _notificationService.SendMessageAsync(telegramId, message);
    }
}

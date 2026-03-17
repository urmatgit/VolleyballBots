using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VolleyballBot.Domain.Enums;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot.Bot.Handlers;

public class StartHandler
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public StartHandler(IUserRepository userRepository, INotificationService notificationService)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    public async Task HandleAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;
        var username = update.Message.From.Username;

        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null)
        {
            await _userRepository.CreateAsync(telegramId, username);
        }

        var message = $"""
            🏐 *Добро пожаловать в Volleyball Bot!*
            
            Я помогу организовать волейбольную игру.
            
            *Команды для игроков:*
            /games - список доступных игр
            /join <id> - записаться на игру
            /leave <id> - отписаться от игры
            /mygames - мои записи
            /players <id> - список игроков
            
            *Команды для админов:*
            /creategame - создать игру
            /admin games - мои созданные игры
            /cancelgame <id> - отменить игру
            
            /help - подробная справка
            """;

        await _notificationService.SendMessageAsync(telegramId, message);
    }
}

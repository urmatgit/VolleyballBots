using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot.Bot.Handlers;

public class HelpHandler
{
    private readonly INotificationService _notificationService;

    public HelpHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task HandleAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;

        var message = $"""
            🏐 *Volleyball Bot - Справка*
            
            *Как записаться на игру:*
            1. Используйте /games для просмотра доступных игр
            2. Нажмите на кнопку "Записаться" или используйте /join <id>
            
            *Как отписаться:*
            - Используйте /leave <id> или кнопку "Отписаться"
            - ⚠️ Отписаться можно не позднее чем за 3 часа до начала
            
            *Статусы записи:*
            🟢 Игрок - вы в основном составе
            🟡 Резерв - вы в списке ожидания
            
            *Создание игры (для админов):*
            1. Используйте /creategame
            2. Следуйте инструкциям бота
            
            *Полезные команды:*
            /games - все доступные игры
            /mygames - игры, на которые вы записаны
            /players <id> - кто идёт на игру
            /help - эта справка
            
            При возникновении вопросов обратитесь к администратору.
            """;

        await _notificationService.SendMessageAsync(telegramId, message);
    }
}

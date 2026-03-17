using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using VolleyballBot.Domain.Entities;
using VolleyballBot.Domain.Enums;
using VolleyballBot.Infrastructure.Repositories;

namespace VolleyballBot.Services;

public interface INotificationService
{
    Task NotifyNewGameAsync(Game game, List<User> players);
    Task NotifyPlayerJoinedAsync(Game game, User player, PlayerStatus status);
    Task NotifyPlayerLeftAsync(Game game, User player);
    Task NotifyGameCancelledAsync(Game game, List<User> players);
    Task NotifyReserveMovedAsync(Game game, User player);
    Task SendMessageAsync(long telegramId, string message);
}

public class NotificationService : INotificationService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserRepository _userRepository;

    public NotificationService(ITelegramBotClient botClient, IUserRepository userRepository)
    {
        _botClient = botClient;
        _userRepository = userRepository;
    }

    public async Task NotifyNewGameAsync(Game game, List<User> players)
    {
        var message = $"""
            🏐 *Новая игра!*
            
            📅 Дата: {game.DateTime:dd.MM.yyyy}
            ⏰ Время: {game.DateTime:HH:mm}
            📍 Адрес: {game.Address}
            👥 Мест: {game.MaxPlayers}
            💰 Цена: {game.Price} ₽
            
            Записывайтесь командой /games
            """;

        foreach (var player in players)
        {
            try
            {
                await _botClient.SendMessage(player.TelegramId, message, parseMode: ParseMode.Markdown);
            }
            catch
            {
                // Игнорируем ошибки отправки (бот заблокирован)
            }
        }
    }

    public async Task NotifyPlayerJoinedAsync(Game game, User player, PlayerStatus status)
    {
        var statusText = status == PlayerStatus.Player ? "игроком" : "резервистом";
        var message = $"""
            🏐 {player.Username ?? "Игрок"} записался на игру как {statusText}
            
            📅 {game.DateTime:dd.MM.yyyy} в {game.DateTime:HH:mm}
            📍 {game.Address}
            """;

        // Уведомляем админа
        try
        {
            var admin = await _userRepository.GetByTelegramIdAsync(game.CreatedBy);
            if (admin != null)
            {
                await _botClient.SendMessage(admin.TelegramId, message, parseMode: ParseMode.Markdown);
            }
        }
        catch
        {
            // Игнорируем ошибки
        }
    }

    public async Task NotifyPlayerLeftAsync(Game game, User player)
    {
        var message = $"""
            ❌ {player.Username ?? "Игрок"} отписался от игры
            
            📅 {game.DateTime:dd.MM.yyyy} в {game.DateTime:HH:mm}
            📍 {game.Address}
            
            Освободилось место!
            """;

        // Уведомляем админа
        try
        {
            var admin = await _userRepository.GetByTelegramIdAsync(game.CreatedBy);
            if (admin != null)
            {
                await _botClient.SendMessage(admin.TelegramId, message, parseMode: ParseMode.Markdown);
            }
        }
        catch
        {
            // Игнорируем ошибки
        }

        // Уведомляем первого из резерва
        var firstReserve = game.GamePlayers
            .Where(gp => gp.Status == Domain.Enums.PlayerStatus.Reserve)
            .OrderBy(gp => gp.JoinedAt)
            .FirstOrDefault();

        if (firstReserve?.User != null)
        {
            try
            {
                await _botClient.SendMessage(
                    firstReserve.User.TelegramId,
                    "🎉 Освободилось место в игре! Вы автоматически переведены из резерва в игроки.",
                    parseMode: ParseMode.Markdown);
            }
            catch
            {
                // Игнорируем ошибки
            }
        }
    }

    public async Task NotifyGameCancelledAsync(Game game, List<User> players)
    {
        var message = $"""
            ❌ *Игра отменена!*
            
            📅 {game.DateTime:dd.MM.yyyy} в {game.DateTime:HH:mm}
            📍 {game.Address}
            
            Приносим извинения за неудобства.
            """;

        foreach (var player in players)
        {
            try
            {
                await _botClient.SendMessage(player.TelegramId, message, parseMode: ParseMode.Markdown);
            }
            catch
            {
                // Игнорируем ошибки
            }
        }
    }

    public async Task NotifyReserveMovedAsync(Game game, User player)
    {
        var message = $"""
            🎉 *Вы переведены из резерва в игроки!*
            
            📅 {game.DateTime:dd.MM.yyyy} в {game.DateTime:HH:mm}
            📍 {game.Address}
            
            Ждём вас на игре!
            """;

        try
        {
            await _botClient.SendMessage(player.TelegramId, message, parseMode: ParseMode.Markdown);
        }
        catch
        {
            // Игнорируем ошибки
        }
    }

    public async Task SendMessageAsync(long telegramId, string message)
    {
        try
        {
            await _botClient.SendMessage(telegramId, message, parseMode: ParseMode.Markdown);
        }
        catch
        {
            // Игнорируем ошибки
        }
    }
}

using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using VolleyballBot.Domain.Enums;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot.Bot.Handlers;

public class AdminHandler
{
    private readonly IGameService _gameService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AdminHandler(
        IGameService gameService,
        INotificationService notificationService,
        IUserRepository userRepository,
        IConfiguration configuration)
    {
        _gameService = gameService;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public bool IsAdmin(long telegramId)
    {
        var adminIds = _configuration.GetSection("AdminTelegramIds").Get<List<long>>() ?? new List<long>();
        return adminIds.Contains(telegramId);
    }

    public async Task HandleCreateGameAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;
        if (!IsAdmin(telegramId))
        {
            await _notificationService.SendMessageAsync(telegramId, "❌ У вас нет прав администратора");
            return;
        }

        var message = """
            🏐 *Создание новой игры*
            
            Пожалуйста, введите данные в формате:
            /newgame <дата> <время> <адрес> <макс.игроков> <цена>
            
            Пример:
            /newgame 20.03.2026 19:00 "ул. Спортивная, 1" 12 500
            
            Или отправьте данные по пунктам:
            1. Дата (ДД.ММ.ГГГГ)
            2. Время (ЧЧ:ММ)
            3. Адрес
            4. Максимальное количество игроков
            5. Цена с игрока
            """;

        await _notificationService.SendMessageAsync(telegramId, message);
    }

    public async Task HandleNewGameAsync(ITelegramBotClient botClient, Update update, string args)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;
        if (!IsAdmin(telegramId))
        {
            await _notificationService.SendMessageAsync(telegramId, "❌ У вас нет прав администратора");
            return;
        }

        // Парсим аргументы: /newgame 20.03.2026 19:00 "ул. Спортивная, 1" 12 500
        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5)
        {
            await _notificationService.SendMessageAsync(telegramId, "❌ Недостаточно параметров. Используйте формат:\n/newgame <дата> <время> <адрес> <макс.игроков> <цена>");
            return;
        }

        try
        {
            var dateStr = parts[0];
            var timeStr = parts[1];
            var address = parts[2].Trim('"');
            if (!int.TryParse(parts[^2], out var maxPlayers))
            {
                await _notificationService.SendMessageAsync(telegramId, "❌ Неверный формат количества игроков");
                return;
            }
            if (!decimal.TryParse(parts[^1], out var price))
            {
                await _notificationService.SendMessageAsync(telegramId, "❌ Неверный формат цены");
                return;
            }

            // Адрес может быть в кавычках и содержать пробелы
            if (parts.Length > 5)
            {
                address = string.Join(" ", parts.Skip(2).Take(parts.Length - 4)).Trim('"');
            }

            if (!DateTime.TryParseExact($"{dateStr} {timeStr}", "dd.MM.yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out var dateTime))
            {
                await _notificationService.SendMessageAsync(telegramId, "❌ Неверный формат даты/времени. Используйте ДД.ММ.ГГГГ ЧЧ:ММ");
                return;
            }

            var user = await _userRepository.GetByTelegramIdAsync(telegramId);
            if (user == null)
            {
                await _notificationService.SendMessageAsync(telegramId, "❌ Пользователь не найден");
                return;
            }

            var game = await _gameService.CreateGameAsync(dateTime, address, maxPlayers, price, user.Id);

            var successMessage = $"""
                ✅ *Игра создана!*
                
                #{game.Id} - {game.DateTime:dd.MM.yyyy} в {game.DateTime:HH:mm}
                📍 {game.Address}
                👥 {game.MaxPlayers} мест
                💰 {game.Price} ₽
                """;

            await _notificationService.SendMessageAsync(telegramId, successMessage);

            // Уведомляем всех игроков о новой игре
            var allPlayers = await _userRepository.GetAllPlayersAsync();
            await _notificationService.NotifyNewGameAsync(game, allPlayers);
        }
        catch (Exception ex)
        {
            await _notificationService.SendMessageAsync(telegramId, $"❌ Ошибка: {ex.Message}");
        }
    }

    public async Task HandleAdminGamesAsync(ITelegramBotClient botClient, Update update)
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

        var games = await _gameService.GetAvailableGamesAsync();
        var myGames = games.Where(g => g.CreatedBy == user.Id).ToList();

        if (myGames.Count == 0)
        {
            await _notificationService.SendMessageAsync(telegramId, "📭 У вас пока нет созданных игр");
            return;
        }

        var message = "🏐 *Ваши созданные игры:*\n\n";

        foreach (var game in myGames)
        {
            var playerCount = game.GamePlayers.Count(gp => gp.Status == PlayerStatus.Player);
            var status = game.IsActive ? "🟢 Активна" : "❌ Отменена";
            message += $"""
                *#{game.Id}* - {game.DateTime:dd.MM.yyyy} в {game.DateTime:HH:mm}
                📍 {game.Address}
                👥 {playerCount}/{game.MaxPlayers} игроков
                💰 {game.Price} ₽
                {status}
                
                """;
        }

        message += "\nДля отмены используйте: /cancelgame <id>";
        await _notificationService.SendMessageAsync(telegramId, message);
    }

    public async Task HandleCancelGameAsync(ITelegramBotClient botClient, Update update, string? gameIdArg)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;
        if (!IsAdmin(telegramId))
        {
            await _notificationService.SendMessageAsync(telegramId, "❌ У вас нет прав администратора");
            return;
        }

        if (string.IsNullOrEmpty(gameIdArg) || !long.TryParse(gameIdArg, out var gameId))
        {
            await _notificationService.SendMessageAsync(telegramId, "❌ Неверный формат. Используйте: /cancelgame <id игры>");
            return;
        }

        var game = await _gameService.CancelGameAsync(gameId);
        if (game == null)
        {
            await _notificationService.SendMessageAsync(telegramId, "❌ Игра не найдена или уже отменена");
            return;
        }

        await _notificationService.SendMessageAsync(telegramId, $"✅ Игра #{gameId} отменена");

        // Уведомляем всех игроков
        var gamePlayers = await _gameService.GetGamePlayersAsync(gameId);
        var players = gamePlayers.Select(gp => gp.User!).Where(u => u != null).ToList();
        await _notificationService.NotifyGameCancelledAsync(game, players);
    }
}

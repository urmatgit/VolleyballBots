using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using VolleyballBot.Bot.Services;
using VolleyballBot.Infrastructure.Repositories;
using VolleyballBot.Services;

namespace VolleyballBot.Bot.Handlers;

/// <summary>
/// Пошаговое создание игры
/// </summary>
public class CreateGameHandler
{
    private readonly IGameService _gameService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IUserStateService _userStateService;
    private readonly IKeyboardService _keyboardService;

    public CreateGameHandler(
        IGameService gameService,
        INotificationService notificationService,
        IUserRepository userRepository,
        IUserStateService userStateService,
        IKeyboardService keyboardService)
    {
        _gameService = gameService;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _userStateService = userStateService;
        _keyboardService = keyboardService;
    }

    public async Task HandleStartCreateGameAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;
        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null)
        {
            await _notificationService.SendMessageAsync(telegramId, "Сначала нажмите /start для регистрации");
            return;
        }

        // Начинаем пошаговый процесс
        _userStateService.SetState(telegramId, UserState.CreatingGame_Date);
        _userStateService.SetGameData(telegramId, new CreateGameData());

        var message = """
            ➕ *Создание новой игры*
            
            *Шаг 1/5: Дата игры*
            
            Введите дату в формате ДД.ММ.ГГГГ
            
            Например: 20.03.2026
            
            /cancel - отменить создание
            """;

        await botClient.SendMessage(telegramId, message, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: new ReplyKeyboardRemove());
    }

    public async Task HandleInputAsync(ITelegramBotClient botClient, Update update, string input)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;
        var state = _userStateService.GetState(telegramId);
        var gameData = _userStateService.GetGameData(telegramId) ?? new CreateGameData();

        switch (state)
        {
            case UserState.CreatingGame_Date:
                await HandleDateInputAsync(botClient, telegramId, input, gameData);
                break;

            case UserState.CreatingGame_Time:
                await HandleTimeInputAsync(botClient, telegramId, input, gameData);
                break;

            case UserState.CreatingGame_Address:
                await HandleAddressInputAsync(botClient, telegramId, input, gameData);
                break;

            case UserState.CreatingGame_MaxPlayers:
                await HandleMaxPlayersInputAsync(botClient, telegramId, input, gameData);
                break;

            case UserState.CreatingGame_Price:
                await HandlePriceInputAsync(botClient, telegramId, input, gameData);
                break;

            default:
                break;
        }
    }

    private async Task HandleDateInputAsync(ITelegramBotClient botClient, long telegramId, string input, CreateGameData gameData)
    {
        if (!DateTime.TryParseExact(input, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
        {
            await botClient.SendMessage(telegramId, "❌ Неверный формат даты. Используйте ДД.ММ.ГГГГ\n\nПопробуйте ещё раз:");
            return;
        }

        if (date.Date < DateTime.UtcNow.Date)
        {
            await botClient.SendMessage(telegramId, "❌ Дата не может быть в прошлом. Введите будущую дату:");
            return;
        }

        gameData.Date = date.Date;
        _userStateService.SetGameData(telegramId, gameData);
        _userStateService.SetState(telegramId, UserState.CreatingGame_Time);

        await botClient.SendMessage(telegramId, $"""
            ✅ Дата: {date:dd.MM.yyyy}
            
            *Шаг 2/5: Время игры*
            
            Введите время в формате ЧЧ:ММ
            
            Например: 19:00
            
            /cancel - отменить создание
            """, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task HandleTimeInputAsync(ITelegramBotClient botClient, long telegramId, string input, CreateGameData gameData)
    {
        if (!TimeSpan.TryParseExact(input, "hh\\:mm", null, out var time))
        {
            await botClient.SendMessage(telegramId, "❌ Неверный формат времени. Используйте ЧЧ:ММ\n\nПопробуйте ещё раз:");
            return;
        }

        gameData.Time = time;
        _userStateService.SetGameData(telegramId, gameData);
        _userStateService.SetState(telegramId, UserState.CreatingGame_Address);

        await botClient.SendMessage(telegramId, $"""
            ✅ Время: {time.Hours:D2}:{time.Minutes:D2}
            
            *Шаг 3/5: Адрес*
            
            Введите адрес проведения игры
            
            Например: ул. Спортивная, 1
            
            /cancel - отменить создание
            """, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task HandleAddressInputAsync(ITelegramBotClient botClient, long telegramId, string input, CreateGameData gameData)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            await botClient.SendMessage(telegramId, "❌ Адрес не может быть пустым. Введите адрес:");
            return;
        }

        gameData.Address = input.Trim();
        _userStateService.SetGameData(telegramId, gameData);
        _userStateService.SetState(telegramId, UserState.CreatingGame_MaxPlayers);

        await botClient.SendMessage(telegramId, $"""
            ✅ Адрес: {input}
            
            *Шаг 4/5: Количество игроков*
            
            Введите максимальное количество игроков (число)
            
            Например: 12
            
            /cancel - отменить создание
            """, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task HandleMaxPlayersInputAsync(ITelegramBotClient botClient, long telegramId, string input, CreateGameData gameData)
    {
        if (!int.TryParse(input, out var maxPlayers) || maxPlayers < 2 || maxPlayers > 50)
        {
            await botClient.SendMessage(telegramId, "❌ Неверное число. Введите число от 2 до 50:");
            return;
        }

        gameData.MaxPlayers = maxPlayers;
        _userStateService.SetGameData(telegramId, gameData);
        _userStateService.SetState(telegramId, UserState.CreatingGame_Price);

        await botClient.SendMessage(telegramId, $"""
            ✅ Количество игроков: {maxPlayers}
            
            *Шаг 5/5: Цена*
            
            Введите стоимость с игрока в рублях (число)
            
            Например: 500
            
            /cancel - отменить создание
            """, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task HandlePriceInputAsync(ITelegramBotClient botClient, long telegramId, string input, CreateGameData gameData)
    {
        if (!decimal.TryParse(input, out var price) || price < 0)
        {
            await botClient.SendMessage(telegramId, "❌ Неверная цена. Введите число >= 0:");
            return;
        }

        gameData.Price = price;

        // Все данные собраны - создаём игру
        var user = await _userRepository.GetByTelegramIdAsync(telegramId);
        if (user == null)
        {
            await _notificationService.SendMessageAsync(telegramId, "❌ Ошибка: пользователь не найден");
            _userStateService.ClearData(telegramId);
            return;
        }

        var dateTime = gameData.Date.Value.Date + gameData.Time.Value;
        var game = await _gameService.CreateGameAsync(dateTime, gameData.Address!, gameData.MaxPlayers!.Value, gameData.Price!.Value, user.Id);

        // Очищаем состояние
        _userStateService.ClearData(telegramId);

        var message = $"""
            ✅ *Игра создана!*
            
            #{game.Id} - {game.DateTime:dd.MM.yyyy} в {game.DateTime:HH:mm}
            📍 {game.Address}
            👥 {game.MaxPlayers} мест
            💰 {game.Price} ₽
            
            Теперь верните меню:
            """;

        await botClient.SendMessage(telegramId, message, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: _keyboardService.GetAdminMenu());

        // Уведомляем всех игроков о новой игре
        var allPlayers = await _userRepository.GetAllPlayersAsync();
        await _notificationService.NotifyNewGameAsync(game, allPlayers);
    }

    public async Task HandleCancelAsync(ITelegramBotClient botClient, Update update)
    {
        if (update.Message == null) return;

        var telegramId = update.Message.From.Id;
        _userStateService.ClearData(telegramId);

        await botClient.SendMessage(telegramId, "❌ Создание игры отменено", replyMarkup: _keyboardService.GetAdminMenu());
    }
}

using Telegram.Bot.Types.ReplyMarkups;

namespace VolleyballBot.Bot.Services;

/// <summary>
/// Сервис клавиатур
/// </summary>
public interface IKeyboardService
{
    ReplyKeyboardMarkup GetMainMenu();
    ReplyKeyboardMarkup GetAdminMenu();
    InlineKeyboardMarkup GetGamesList(List<Domain.Entities.Game> games);
    InlineKeyboardMarkup GetGameDetails(long gameId, string gameName, bool isJoined, Domain.Enums.PlayerStatus? status);
    InlineKeyboardMarkup GetMyGamesList(List<Domain.Entities.Game> games);
}

public class KeyboardService : IKeyboardService
{
    public ReplyKeyboardMarkup GetMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[]
            {
                new KeyboardButton("🏐 Доступные игры"),
                new KeyboardButton("📋 Мои записи")
            },
            new[]
            {
                new KeyboardButton("ℹ️ Помощь")
            }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
    }

    public ReplyKeyboardMarkup GetAdminMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[]
            {
                new KeyboardButton("🏐 Доступные игры"),
                new KeyboardButton("📋 Мои записи")
            },
            new[]
            {
                new KeyboardButton("➕ Создать игру"),
                new KeyboardButton("📊 Мои игры")
            },
            new[]
            {
                new KeyboardButton("ℹ️ Помощь")
            }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
    }

    public InlineKeyboardMarkup GetGamesList(List<Domain.Entities.Game> games)
    {
        var buttons = games.Select(g => new[]
        {
            InlineKeyboardButton.WithCallbackData(
                $"📅 {g.DateTime:dd.MM} {g.DateTime:HH:mm} | {g.GamePlayers.Count(gp => gp.Status == Domain.Enums.PlayerStatus.Player)}/{g.MaxPlayers}",
                $"view_game_{g.Id}")
        }).ToArray();

        return new InlineKeyboardMarkup(buttons);
    }

    public InlineKeyboardMarkup GetGameDetails(long gameId, string gameName, bool isJoined, Domain.Enums.PlayerStatus? status)
    {
        var buttons = new List<InlineKeyboardButton>();

        if (isJoined)
        {
            var statusText = status == Domain.Enums.PlayerStatus.Reserve ? "🟡 Резерв" : "✅ Игрок";
            buttons.Add(InlineKeyboardButton.WithCallbackData($"{statusText} | Отписаться", $"leave_game_{gameId}"));
        }
        else
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("🟢 Записаться", $"join_game_{gameId}"));
        }

        buttons.Add(InlineKeyboardButton.WithCallbackData("🔙 Назад", "games_list"));

        return new InlineKeyboardMarkup(buttons.ToArray());
    }

    public InlineKeyboardMarkup GetMyGamesList(List<Domain.Entities.Game> games)
    {
        var buttons = games.Select(g => new[]
        {
            InlineKeyboardButton.WithCallbackData(
                $"📅 {g.DateTime:dd.MM.yyyy} {g.DateTime:HH:mm}",
                $"view_game_{g.Id}")
        }).ToArray();

        buttons = buttons.Concat(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "main_menu") } }).ToArray();

        return new InlineKeyboardMarkup(buttons);
    }
}

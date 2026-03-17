namespace VolleyballBot.Bot.Services;

/// <summary>
/// Состояния пользователя для пошаговых сценариев
/// </summary>
public enum UserState
{
    None,
    CreatingGame_Date,
    CreatingGame_Time,
    CreatingGame_Address,
    CreatingGame_MaxPlayers,
    CreatingGame_Price
}

/// <summary>
/// Данные создаваемой игры
/// </summary>
public class CreateGameData
{
    public DateTime? Date { get; set; }
    public TimeSpan? Time { get; set; }
    public string? Address { get; set; }
    public int? MaxPlayers { get; set; }
    public decimal? Price { get; set; }
}

/// <summary>
/// Сервис управления состояниями пользователей
/// </summary>
public interface IUserStateService
{
    UserState GetState(long telegramId);
    void SetState(long telegramId, UserState state);
    CreateGameData? GetGameData(long telegramId);
    void SetGameData(long telegramId, CreateGameData data);
    void ClearData(long telegramId);
}

public class UserStateService : IUserStateService
{
    private readonly Dictionary<long, UserState> _states = new();
    private readonly Dictionary<long, CreateGameData> _gameData = new();

    public UserState GetState(long telegramId)
    {
        return _states.TryGetValue(telegramId, out var state) ? state : UserState.None;
    }

    public void SetState(long telegramId, UserState state)
    {
        _states[telegramId] = state;
    }

    public CreateGameData? GetGameData(long telegramId)
    {
        _gameData.TryGetValue(telegramId, out var data);
        return data;
    }

    public void SetGameData(long telegramId, CreateGameData data)
    {
        _gameData[telegramId] = data;
    }

    public void ClearData(long telegramId)
    {
        _states.Remove(telegramId);
        _gameData.Remove(telegramId);
    }
}

using VolleyballBot.Domain.Entities;
using VolleyballBot.Domain.Enums;
using VolleyballBot.Infrastructure.Repositories;

namespace VolleyballBot.Services;

public interface IGameService
{
    Task<Game> CreateGameAsync(DateTime dateTime, string address, int maxPlayers, decimal price, long adminId);
    Task<(bool Success, string Message, PlayerStatus Status)> JoinGameAsync(long gameId, long userId);
    Task<(bool Success, string Message)> LeaveGameAsync(long gameId, long userId);
    Task<Game?> CancelGameAsync(long gameId);
    Task<List<Game>> GetAvailableGamesAsync();
    Task<List<Game>> GetMyGamesAsync(long userId);
    Task<int> GetPlayerCountAsync(long gameId);
    Task<List<GamePlayer>> GetGamePlayersAsync(long gameId);
}

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IUserRepository _userRepository;

    public GameService(IGameRepository gameRepository, IUserRepository userRepository)
    {
        _gameRepository = gameRepository;
        _userRepository = userRepository;
    }

    public async Task<Game> CreateGameAsync(DateTime dateTime, string address, int maxPlayers, decimal price, long adminId)
    {
        var game = new Game
        {
            DateTime = dateTime,
            Address = address,
            MaxPlayers = maxPlayers,
            Price = price,
            CreatedBy = adminId,
            IsActive = true
        };

        return await _gameRepository.CreateAsync(game);
    }

    public async Task<(bool Success, string Message, PlayerStatus Status)> JoinGameAsync(long gameId, long userId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null)
        {
            return (false, "Игра не найдена", PlayerStatus.Player);
        }

        if (!game.IsActive)
        {
            return (false, "Игра уже отменена", PlayerStatus.Player);
        }

        if (game.DateTime <= DateTime.UtcNow)
        {
            return (false, "Игра уже началась или прошла", PlayerStatus.Player);
        }

        // Проверяем, не записан ли уже пользователь
        var existingEntry = game.GamePlayers.FirstOrDefault(gp => gp.UserId == userId);
        if (existingEntry != null)
        {
            return (false, "Вы уже записаны на эту игру", existingEntry.Status);
        }

        // Считаем количество игроков (не резерв)
        var playerCount = game.GamePlayers.Count(gp => gp.Status == PlayerStatus.Player);

        var status = playerCount < game.MaxPlayers ? PlayerStatus.Player : PlayerStatus.Reserve;

        var gamePlayer = new GamePlayer
        {
            GameId = gameId,
            UserId = userId,
            Status = status,
            JoinedAt = DateTime.UtcNow
        };

        game.GamePlayers.Add(gamePlayer);
        await _gameRepository.UpdateAsync(game);

        var message = status == PlayerStatus.Player
            ? "Вы записаны на игру!"
            : "Места заполнены. Вы записаны в резерв.";

        return (true, message, status);
    }

    public async Task<(bool Success, string Message)> LeaveGameAsync(long gameId, long userId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null)
        {
            return (false, "Игра не найдена");
        }

        var gamePlayer = game.GamePlayers.FirstOrDefault(gp => gp.UserId == userId);
        if (gamePlayer == null)
        {
            return (false, "Вы не записаны на эту игру");
        }

        // Проверяем, не позднее ли 3 часов до начала
        var timeUntilGame = game.DateTime - DateTime.UtcNow;
        if (timeUntilGame.TotalHours < 3)
        {
            return (false, "Отписаться можно не позднее чем за 3 часа до начала игры");
        }

        game.GamePlayers.Remove(gamePlayer);
        await _gameRepository.UpdateAsync(game);

        return (true, "Вы успешно отписались от игры");
    }

    public async Task<Game?> CancelGameAsync(long gameId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null || !game.IsActive)
        {
            return null;
        }

        game.IsActive = false;
        return await _gameRepository.UpdateAsync(game);
    }

    public async Task<List<Game>> GetAvailableGamesAsync()
    {
        return await _gameRepository.GetActiveGamesAsync();
    }

    public async Task<List<Game>> GetMyGamesAsync(long userId)
    {
        var allGames = await _gameRepository.GetActiveGamesAsync();
        return allGames.Where(g => g.GamePlayers.Any(gp => gp.UserId == userId)).ToList();
    }

    public async Task<int> GetPlayerCountAsync(long gameId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        return game?.GamePlayers.Count(gp => gp.Status == PlayerStatus.Player) ?? 0;
    }

    public async Task<List<GamePlayer>> GetGamePlayersAsync(long gameId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        return game?.GamePlayers.ToList() ?? new List<GamePlayer>();
    }
}

using VolleyballBot.Domain.Enums;

namespace VolleyballBot.Domain.Entities;

public class User
{
    public long Id { get; set; }
    public long TelegramId { get; set; }
    public string? Username { get; set; }
    public UserRole Role { get; set; } = UserRole.Player;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GamePlayer> GamePlayers { get; set; } = new List<GamePlayer>();
    public ICollection<Game> CreatedGames { get; set; } = new List<Game>();
}

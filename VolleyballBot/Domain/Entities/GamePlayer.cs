using VolleyballBot.Domain.Enums;

namespace VolleyballBot.Domain.Entities;

public class GamePlayer
{
    public long Id { get; set; }
    public long GameId { get; set; }
    public long UserId { get; set; }
    public PlayerStatus Status { get; set; } = PlayerStatus.Player;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Game? Game { get; set; }
    public User? User { get; set; }
}

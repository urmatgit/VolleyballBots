namespace VolleyballBot.Domain.Entities;

public class Game
{
    public long Id { get; set; }
    public DateTime DateTime { get; set; }
    public string Address { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public decimal Price { get; set; }
    public long CreatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? Creator { get; set; }
    public ICollection<GamePlayer> GamePlayers { get; set; } = new List<GamePlayer>();
}

using Microsoft.EntityFrameworkCore;
using VolleyballBot.Domain.Entities;

namespace VolleyballBot.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GamePlayer> GamePlayers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TelegramId).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(256);
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Address).HasMaxLength(512).IsRequired();
            entity.HasOne<User>()
                .WithMany(u => u.CreatedGames)
                .HasForeignKey(g => g.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GamePlayer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.GameId, e.UserId }).IsUnique();
            entity.HasOne(gp => gp.Game)
                .WithMany(g => g.GamePlayers)
                .HasForeignKey(gp => gp.GameId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(gp => gp.User)
                .WithMany(u => u.GamePlayers)
                .HasForeignKey(gp => gp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

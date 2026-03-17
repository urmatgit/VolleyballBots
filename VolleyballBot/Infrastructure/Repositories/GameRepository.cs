using Microsoft.EntityFrameworkCore;
using VolleyballBot.Domain.Entities;
using VolleyballBot.Infrastructure.Database;

namespace VolleyballBot.Infrastructure.Repositories;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(long id);
    Task<List<Game>> GetActiveGamesAsync();
    Task<List<Game>> GetGamesByAdminAsync(long adminId);
    Task<Game> CreateAsync(Game game);
    Task<Game> UpdateAsync(Game game);
    Task DeleteAsync(long id);
}

public class GameRepository : IGameRepository
{
    private readonly AppDbContext _context;

    public GameRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Game?> GetByIdAsync(long id)
    {
        return await _context.Games
            .Include(g => g.GamePlayers)
            .ThenInclude(gp => gp.User)
            .Include(g => g.Creator)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<List<Game>> GetActiveGamesAsync()
    {
        return await _context.Games
            .Include(g => g.GamePlayers)
            .ThenInclude(gp => gp.User)
            .Include(g => g.Creator)
            .Where(g => g.IsActive && g.DateTime > DateTime.UtcNow)
            .OrderBy(g => g.DateTime)
            .ToListAsync();
    }

    public async Task<List<Game>> GetGamesByAdminAsync(long adminId)
    {
        return await _context.Games
            .Include(g => g.GamePlayers)
            .ThenInclude(gp => gp.User)
            .Where(g => g.CreatedBy == adminId)
            .OrderByDescending(g => g.DateTime)
            .ToListAsync();
    }

    public async Task<Game> CreateAsync(Game game)
    {
        _context.Games.Add(game);
        await _context.SaveChangesAsync();
        return game;
    }

    public async Task<Game> UpdateAsync(Game game)
    {
        _context.Games.Update(game);
        await _context.SaveChangesAsync();
        return game;
    }

    public async Task DeleteAsync(long id)
    {
        var game = await _context.Games.FindAsync(id);
        if (game != null)
        {
            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
        }
    }
}

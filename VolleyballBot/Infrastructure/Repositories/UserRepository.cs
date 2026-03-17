using Microsoft.EntityFrameworkCore;
using VolleyballBot.Domain.Entities;
using VolleyballBot.Infrastructure.Database;

namespace VolleyballBot.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<User?> GetByTelegramIdAsync(long telegramId);
    Task<User> CreateAsync(long telegramId, string? username);
    Task<User> UpdateAsync(User user);
    Task<List<User>> GetAllPlayersAsync();
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByTelegramIdAsync(long telegramId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
    }

    public async Task<User> CreateAsync(long telegramId, string? username)
    {
        var user = new User
        {
            TelegramId = telegramId,
            Username = username,
            Role = Domain.Enums.UserRole.Player
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<List<User>> GetAllPlayersAsync()
    {
        return await _context.Users.ToListAsync();
    }
}

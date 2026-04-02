using BuddyScript.Backend.Data;
using BuddyScript.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace BuddyScript.Backend.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id) => await _context.Users.FindAsync(id);

    public async Task<User?> GetByEmailAsync(string email) => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<bool> ExistsByEmailAsync(string email) => await _context.Users.AnyAsync(u => u.Email == email);

    public async Task AddAsync(User user) => _context.Users.Add(user);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

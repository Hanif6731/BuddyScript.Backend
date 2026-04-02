using BuddyScript.Backend.Data;
using BuddyScript.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace BuddyScript.Backend.Repositories;

public class LikeRepository : ILikeRepository
{
    private readonly ApplicationDbContext _context;

    public LikeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Like?> GetLikeAsync(int userId, int entityId, EntityType entityType) =>
        await _context.Likes
            .FirstOrDefaultAsync(l => l.UserId == userId && l.EntityId == entityId && l.EntityType == entityType);

    public IQueryable<Like> GetLikesByEntity(int entityId, EntityType entityType) =>
        _context.Likes
            .Include(l => l.User)
            .Where(l => l.EntityId == entityId && l.EntityType == entityType);

    public async Task AddAsync(Like like) => await _context.Likes.AddAsync(like);

    public void Remove(Like like) => _context.Likes.Remove(like);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

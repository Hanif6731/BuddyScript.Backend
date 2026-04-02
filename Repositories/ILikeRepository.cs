using BuddyScript.Backend.Models;

namespace BuddyScript.Backend.Repositories;

public interface ILikeRepository
{
    Task<Like?> GetLikeAsync(int userId, int entityId, EntityType entityType);
    IQueryable<Like> GetLikesByEntity(int entityId, EntityType entityType);
    Task AddAsync(Like like);
    void Remove(Like like);
    Task SaveChangesAsync();
}

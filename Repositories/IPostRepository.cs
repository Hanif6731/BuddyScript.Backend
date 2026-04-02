using BuddyScript.Backend.Models;

namespace BuddyScript.Backend.Repositories;

public interface IPostRepository
{
    Task AddAsync(Post post);
    Task SaveChangesAsync();
    Task<Post?> GetByIdAsync(int id);
    IQueryable<Post> GetFeedPosts(int userId, int page, int pageSize);
}

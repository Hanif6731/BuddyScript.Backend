using BuddyScript.Backend.Data;
using BuddyScript.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace BuddyScript.Backend.Repositories;

public class PostRepository : IPostRepository
{
    private readonly ApplicationDbContext _context;

    public PostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Post post) => _context.Posts.Add(post);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    public async Task<Post?> GetByIdAsync(int id) => await _context.Posts.FindAsync(id);

    public IQueryable<Post> GetFeedPosts(int userId, int page, int pageSize) =>
        _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Where(p => p.IsPublic || p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery();
}

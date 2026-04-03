using BuddyScript.Backend.Data;
using BuddyScript.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace BuddyScript.Backend.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly ApplicationDbContext _context;

    public CommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Comment comment) => _context.Comments.Add(comment);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    public IQueryable<Comment> GetCommentsForPost(int postId) =>
        _context.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt);

    public IQueryable<Comment> GetTopLevelComments(int postId) =>
        _context.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .OrderBy(c => c.CreatedAt);

    public IQueryable<Comment> GetRepliesForComment(int commentId) =>
        _context.Comments
            .Include(c => c.User)
            .Where(c => c.ParentCommentId == commentId)
            .OrderBy(c => c.CreatedAt);

    public IQueryable<Comment> GetRepliesForComments(IEnumerable<int> parentIds) =>
        _context.Comments
            .Include(c => c.User)
            .Where(c => c.ParentCommentId != null && parentIds.Contains(c.ParentCommentId!.Value))
            .OrderBy(c => c.CreatedAt);
}

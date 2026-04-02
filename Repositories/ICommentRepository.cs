using BuddyScript.Backend.Models;

namespace BuddyScript.Backend.Repositories;

public interface ICommentRepository
{
    Task AddAsync(Comment comment);
    Task SaveChangesAsync();
    IQueryable<Comment> GetCommentsForPost(int postId);
    IQueryable<Comment> GetTopLevelComments(int postId);
    IQueryable<Comment> GetRepliesForComment(int commentId);
    IQueryable<Comment> GetRepliesForComments(IEnumerable<int> parentIds);
}

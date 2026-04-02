using BuddyScript.Backend.DTOs;

namespace BuddyScript.Backend.Services;

public interface IInteractionsService
{
    Task<int> CreateCommentAsync(int userId, CreateCommentDto dto);
    Task<List<CommentResponseDto>> GetCommentsHierarchicalAsync(int postId, int userId);
    Task<List<CommentResponseDto>> GetTopLevelCommentsAsync(int postId, int userId);
    Task<List<CommentResponseDto>> GetRepliesAsync(int commentId, int userId);
    Task<bool> ToggleLikeAsync(int userId, LikeDto dto);
    Task<List<LikerDto>> GetLikersAsync(int entityId, int entityType);
}

using BuddyScript.Backend.DTOs;

namespace BuddyScript.Backend.Services;

public interface IFeedService
{
    Task<int> CreatePostAsync(int userId, PostCreateDto dto);
    Task<List<PostResponseDto>> GetFeedAsync(int userId, int page, int pageSize);
    Task<(byte[]? data, string? mimeType)> GetPostImageAsync(int postId);
}

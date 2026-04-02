using BuddyScript.Backend.Data;
using BuddyScript.Backend.DTOs;
using BuddyScript.Backend.Models;
using BuddyScript.Backend.Repositories;
using BuddyScript.Backend.Utils;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Data;

namespace BuddyScript.Backend.Services;

public class FeedService : IFeedService
{
    private readonly IPostRepository _postRepository;
    private readonly ApplicationDbContext _context;

    public FeedService(IPostRepository postRepository, ApplicationDbContext context)
    {
        _postRepository = postRepository;
        _context = context;
    }

    public async Task<int> CreatePostAsync(int userId, PostCreateDto dto)
    {
        byte[]? compressedImageData = null;
        string? mimeType = null;

        if (dto.Image != null && dto.Image.Length > 0)
        {
            using var stream = dto.Image.OpenReadStream();
            using var image = await Image.LoadAsync(stream);

            int width  = image.Width > 1200 ? 1200 : image.Width;
            int height = (int)((double)image.Height / image.Width * width);
            image.Mutate(x => x.Resize(width, height));

            using var ms = new MemoryStream();
            await image.SaveAsync(ms, new JpegEncoder { Quality = 80 });
            compressedImageData = ms.ToArray();
            mimeType = "image/jpeg";
        }

        var post = new Post
        {
            Content       = InputSanitizer.SanitizeNullable(dto.Content),
            ImageData     = compressedImageData,
            ImageMimeType = mimeType,
            IsPublic      = dto.IsPublic,
            UserId        = userId,
            CreatedAt     = DateTime.UtcNow
        };

        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await _postRepository.AddAsync(post);
            await _postRepository.SaveChangesAsync();
            await tx.CommitAsync();
            return post.Id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<PostResponseDto>> GetFeedAsync(int userId, int page, int pageSize)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var posts = await _postRepository.GetFeedPosts(userId, page, pageSize).ToListAsync();

        return posts.Select(p => new PostResponseDto
        {
            Id           = p.Id,
            Content      = p.Content,
            ImageUrl     = p.ImageData != null ? $"/api/feed/image/{p.Id}" : null,
            IsPublic     = p.IsPublic,
            CreatedAt    = p.CreatedAt,
            UserId       = p.UserId,
            UserFullName = $"{p.User!.FirstName} {p.User.LastName}",
            LikeCount    = p.Likes.Count,
            IsLikedByMe  = p.Likes.Any(l => l.UserId == userId),
            CommentCount = p.Comments.Count
        }).ToList();
    }

    public async Task<(byte[]? data, string? mimeType)> GetPostImageAsync(int postId)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null || post.ImageData == null || post.ImageMimeType == null)
            return (null, null);

        return (post.ImageData, post.ImageMimeType);
    }
}

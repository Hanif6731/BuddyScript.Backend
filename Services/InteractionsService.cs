using BuddyScript.Backend.Data;
using BuddyScript.Backend.DTOs;
using BuddyScript.Backend.Models;
using BuddyScript.Backend.Repositories;
using BuddyScript.Backend.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data;

namespace BuddyScript.Backend.Services;

public class InteractionsService : IInteractionsService
{
    private readonly ICommentRepository _commentRepository;
    private readonly ILikeRepository _likeRepository;
    private readonly ApplicationDbContext _context;

    public InteractionsService(
        ICommentRepository commentRepository,
        ILikeRepository likeRepository,
        ApplicationDbContext context)
    {
        _commentRepository = commentRepository;
        _likeRepository    = likeRepository;
        _context           = context;
    }

    public async Task<int> CreateCommentAsync(int userId, CreateCommentDto dto)
    {
        var comment = new Comment
        {
            Content         = InputSanitizer.Sanitize(dto.Content),
            PostId          = dto.PostId,
            ParentCommentId = dto.ParentCommentId,
            UserId          = userId,
            CreatedAt       = DateTime.UtcNow
        };

        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await _commentRepository.AddAsync(comment);
            await _commentRepository.SaveChangesAsync();
            await tx.CommitAsync();
            return comment.Id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<CommentResponseDto>> GetCommentsHierarchicalAsync(int postId, int userId)
    {
        var comments = await _commentRepository.GetCommentsForPost(postId).ToListAsync();
        var commentIds = comments.Select(c => c.Id).ToList();
        var likes = await _context.Likes
            .Where(l => l.EntityType == EntityType.Comment && commentIds.Contains(l.EntityId))
            .ToListAsync();
        var likesByCommentId = likes.ToLookup(l => l.EntityId);

        var dtos = comments.Select(c => new CommentResponseDto
        {
            Id              = c.Id,
            Content         = c.Content,
            PostId          = c.PostId,
            UserId          = c.UserId,
            UserFullName    = $"{c.User!.FirstName} {c.User.LastName}",
            CreatedAt       = c.CreatedAt,
            ParentCommentId = c.ParentCommentId,
            LikeCount       = likesByCommentId[c.Id].Count(),
            IsLikedByMe     = likesByCommentId[c.Id].Any(l => l.UserId == userId),
            MyReaction      = likesByCommentId[c.Id].FirstOrDefault(l => l.UserId == userId)?.ReactionType
        }).ToList();

        var lookup = dtos.ToLookup(c => c.ParentCommentId);
        foreach (var c in dtos)
            c.Replies = lookup[c.Id].ToList();

        return lookup[null].ToList();
    }

    public async Task<List<CommentResponseDto>> GetTopLevelCommentsAsync(int postId, int userId)
    {
        var allComments = await _commentRepository.GetCommentsForPost(postId).ToListAsync();
        var commentIds = allComments.Select(c => c.Id).ToList();
        var likes = await _context.Likes
            .Where(l => l.EntityType == EntityType.Comment && commentIds.Contains(l.EntityId))
            .ToListAsync();
        var likesByCommentId = likes.ToLookup(l => l.EntityId);

        var replyCounts = allComments
            .Where(c => c.ParentCommentId != null)
            .GroupBy(c => c.ParentCommentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        return allComments
            .Where(c => c.ParentCommentId == null)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponseDto
            {
                Id              = c.Id,
                Content         = c.Content,
                PostId          = c.PostId,
                UserId          = c.UserId,
                UserFullName    = $"{c.User!.FirstName} {c.User.LastName}",
                CreatedAt       = c.CreatedAt,
                ParentCommentId = null,
                LikeCount       = likesByCommentId[c.Id].Count(),
                IsLikedByMe     = likesByCommentId[c.Id].Any(l => l.UserId == userId),
                MyReaction      = likesByCommentId[c.Id].FirstOrDefault(l => l.UserId == userId)?.ReactionType,
                ReplyCount      = replyCounts.GetValueOrDefault(c.Id, 0),
                Replies         = new List<CommentResponseDto>()
            }).ToList();
    }

    public async Task<List<CommentResponseDto>> GetRepliesAsync(int commentId, int userId)
    {
        var directReplies = await _commentRepository.GetRepliesForComment(commentId).ToListAsync();
        var replyIds = directReplies.Select(r => r.Id).ToList();

        var likes = await _context.Likes
            .Where(l => l.EntityType == EntityType.Comment && replyIds.Contains(l.EntityId))
            .ToListAsync();
        var likesByCommentId = likes.ToLookup(l => l.EntityId);

        var grandchildCounts = await _commentRepository
            .GetRepliesForComments(replyIds)
            .GroupBy(c => c.ParentCommentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToListAsync();

        var replyCounts = grandchildCounts.ToDictionary(x => x.ParentId, x => x.Count);

        return directReplies.Select(c => new CommentResponseDto
        {
            Id              = c.Id,
            Content         = c.Content,
            PostId          = c.PostId,
            UserId          = c.UserId,
            UserFullName    = $"{c.User!.FirstName} {c.User.LastName}",
            CreatedAt       = c.CreatedAt,
            ParentCommentId = c.ParentCommentId,
            LikeCount       = likesByCommentId[c.Id].Count(),
            IsLikedByMe     = likesByCommentId[c.Id].Any(l => l.UserId == userId),
            MyReaction      = likesByCommentId[c.Id].FirstOrDefault(l => l.UserId == userId)?.ReactionType,
            ReplyCount      = replyCounts.GetValueOrDefault(c.Id, 0),
            Replies         = new List<CommentResponseDto>()
        }).ToList();
    }

    public async Task<string?> ToggleLikeAsync(int userId, LikeDto dto)
    {
        var entityType = (EntityType)dto.EntityType;
        var reactionType = string.IsNullOrWhiteSpace(dto.ReactionType) ? "Like" : dto.ReactionType;

        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var existingLike = await _likeRepository.GetLikeAsync(userId, dto.EntityId, entityType);

            string? result;
            if (existingLike != null)
            {
                if (existingLike.ReactionType == reactionType)
                {
                    _likeRepository.Remove(existingLike);
                    result = null;
                }
                else
                {
                    existingLike.ReactionType = reactionType;
                    result = reactionType;
                }
            }
            else
            {
                await _likeRepository.AddAsync(new Like
                {
                    UserId       = userId,
                    EntityId     = dto.EntityId,
                    EntityType   = entityType,
                    ReactionType = reactionType
                });
                result = reactionType;
            }

            await _likeRepository.SaveChangesAsync();
            await tx.CommitAsync();
            return result;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<LikerDto>> GetLikersAsync(int entityId, int entityType)
    {
        var likes = await _likeRepository
            .GetLikesByEntity(entityId, (EntityType)entityType)
            .ToListAsync();

        return likes.Select(l => new LikerDto
        {
            UserId   = l.UserId,
            FullName = $"{l.User!.FirstName} {l.User.LastName}"
        }).ToList();
    }
}

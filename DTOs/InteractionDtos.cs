using System.ComponentModel.DataAnnotations;

namespace BuddyScript.Backend.DTOs;

public class CreateCommentDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PostId must be a positive integer.")]
    public int PostId { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Comment content is required.")]
    [MaxLength(2000, ErrorMessage = "Comment content cannot exceed 2000 characters.")]
    public string Content { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "ParentCommentId must be a positive integer.")]
    public int? ParentCommentId { get; set; }
}

public class LikeDto
{
    [Range(1, int.MaxValue, ErrorMessage = "EntityId must be a positive integer.")]
    public int EntityId { get; set; }

    [Range(0, 1, ErrorMessage = "EntityType must be 0 (Post) or 1 (Comment).")]
    public int EntityType { get; set; }
}

public class CommentResponseDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int PostId { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? ParentCommentId { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByMe { get; set; }
    public int ReplyCount { get; set; }
    public List<CommentResponseDto> Replies { get; set; } = new();
}

public class LikerDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
}

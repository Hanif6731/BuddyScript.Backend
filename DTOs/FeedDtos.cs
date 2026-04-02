using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BuddyScript.Backend.DTOs;

public class PostCreateDto : IValidatableObject
{
    [MaxLength(5000, ErrorMessage = "Post content cannot exceed 5000 characters.")]
    public string? Content { get; set; }

    public IFormFile? Image { get; set; }

    public bool IsPublic { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Content) && Image == null)
            yield return new ValidationResult(
                "A post must contain text content, an image, or both.",
                new[] { nameof(Content), nameof(Image) });

        if (Image != null)
        {
            const long maxBytes = 10L * 1024 * 1024;
            if (Image.Length > maxBytes)
                yield return new ValidationResult(
                    "Image file size must not exceed 10 MB.",
                    new[] { nameof(Image) });

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
            };
            if (!allowed.Contains(Image.ContentType))
                yield return new ValidationResult(
                    "Only JPEG, PNG, GIF, and WebP images are supported.",
                    new[] { nameof(Image) });
        }
    }
}

public class PostResponseDto
{
    public int Id { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public bool IsLikedByMe { get; set; }
    public int CommentCount { get; set; }
}

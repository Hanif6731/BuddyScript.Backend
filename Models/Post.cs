using System.ComponentModel.DataAnnotations;
namespace BuddyScript.Backend.Models;
public class Post
{
    [Key]
    public int Id { get; set; }
    public string? Content { get; set; }
    public byte[]? ImageData { get; set; }
    public string? ImageMimeType { get; set; }
    public bool IsPublic { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User? User { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

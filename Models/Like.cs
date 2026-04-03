using System.ComponentModel.DataAnnotations;
namespace BuddyScript.Backend.Models;
public enum EntityType { Post, Comment }
public class Like
{
    [Key]
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int EntityId { get; set; }
    public EntityType EntityType { get; set; }
    [MaxLength(20)]
    public string ReactionType { get; set; } = "Like";
}

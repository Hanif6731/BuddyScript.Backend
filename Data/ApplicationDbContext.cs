using Microsoft.EntityFrameworkCore;
namespace BuddyScript.Backend.Data;
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    public DbSet<Models.User> Users { get; set; }
    public DbSet<Models.Post> Posts { get; set; }
    public DbSet<Models.Comment> Comments { get; set; }
    public DbSet<Models.Like> Likes { get; set; }
    public DbSet<Models.UserSettings> UserSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Models.User>()
            .HasIndex(u => u.Email)
            .IsUnique();
            
        modelBuilder.Entity<Models.Comment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Models.Post>()
            .HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Models.Like>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Models.Post>()
            .HasIndex(p => new { p.IsPublic, p.CreatedAt })
            .IsDescending(false, true);

        modelBuilder.Entity<Models.Like>()
            .HasIndex(l => new { l.EntityId, l.EntityType });

        modelBuilder.Entity<Models.Like>()
            .HasIndex(l => new { l.UserId, l.EntityId, l.EntityType })
            .IsUnique();

        modelBuilder.Entity<Models.Comment>()
            .HasIndex(c => c.PostId);

        modelBuilder.Entity<Models.Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Models.UserSettings>()
            .HasOne(s => s.User)
            .WithOne()
            .HasForeignKey<Models.UserSettings>(s => s.UserId);
    }
}

using Microsoft.AspNetCore.Identity;
using MiniBlog.Models.Entities;

public enum PostStatus
{
    Pending,
    Approved,
    Rejected
}

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AuthorId { get; set; }
    public IdentityUser Author { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PostStatus Status { get; set; } = PostStatus.Pending;

    public List<Comment> Comments { get; set; }
}
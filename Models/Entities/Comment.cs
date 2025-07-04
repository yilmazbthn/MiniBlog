using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MiniBlog.Models.Entities;

public class Comment
{
    public int Id { get; set; }
    [Required,MaxLength(300)]
    public string Text { get; set; } 
    public int PostId { get; set; }
    public Post Post { get; set; }

    public string AuthorId { get; set; }
    public IdentityUser Author { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
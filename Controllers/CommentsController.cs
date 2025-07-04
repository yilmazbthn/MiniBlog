using FluentEmail.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniBlog.Data;
using MiniBlog.Models.Entities;

namespace MiniBlog.Controllers;

public class CommentsController(AppDbContext context,IEmailSender emailSender,IFluentEmail fluentEmail,UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager):ControllerBase
{
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComment([FromBody] Comment model)
    {
        if (string.IsNullOrWhiteSpace(model.Text) || model.Text.Length > 300)
            return BadRequest("Yorum 300 karakteri Geçmemelidir.");

        var user = await userManager.GetUserAsync(User);
        var post = await context.Posts.Include(p => p.Author).FirstOrDefaultAsync(p => p.Id == model.PostId);

        if (user == null || post == null)
            return BadRequest("Kullanıcı veya yazı bulunamadı.");

        var comment = new Comment
        {
            Text = model.Text,
            AuthorId = user.Id,
            PostId = model.PostId,
            CreatedAt = DateTime.UtcNow
        };

        context.Comments.Add(comment);
        await context.SaveChangesAsync();
        
        await emailSender.SendEmailAsync(
            post.Author.Email,
            "Yeni Yorum Geldi",
            $"Yazınıza yeni bir yorum yapıldı: \"{model.Text}\"");

        return Ok(new { message = "Yorum eklendi ve e-posta gönderildi." });
    }
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var comment = await context.Comments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comment == null)
            return NotFound("Yorum bulunamadı.");

        var user = await userManager.GetUserAsync(User);
        var isOwner = comment.AuthorId == user.Id;
        var isPostOwner = comment.Post.AuthorId == user.Id;

        if (!isOwner && !isPostOwner)
            return Forbid("Yorumu silme yetkiniz yok.");

        context.Comments.Remove(comment);
        await context.SaveChangesAsync();

        return NoContent();
    }
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SearchPosts([FromQuery] string? q)
    {
        var query = context.Posts
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p =>
                p.Title.Contains(q) || p.Content.Contains(q));
        }

        var results = await query
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Content,
                Author = p.Author.UserName,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(results);
    }
    [HttpGet]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> GetAllComments()
    {
        var comments = await context.Comments
            .Include(c => c.Author)
            .Include(c => c.Post)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Text,
                Author = c.Author.UserName,
                PostTitle = c.Post.Title,
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(comments);
    }

    
}
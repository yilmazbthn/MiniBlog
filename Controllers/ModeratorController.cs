using FluentEmail.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniBlog.Data;

namespace MiniBlog.Controllers;
[ApiController]
[Route("[controller]")]
public class ModeratorController(AppDbContext context,IEmailSender emailSender,IFluentEmail fluentEmail,UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager):ControllerBase
{
    [HttpGet("pending-posts")]
    public async Task<IActionResult> GetPendingPosts()
    {
        var posts = await context.Posts
            .Include(p => p.Author)
            .Where(p => p.Status == PostStatus.Pending)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Content,
                Author = p.Author.UserName,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(posts);
    }
    
    [HttpPost("approve-post/{id}")]
    public async Task<IActionResult> ApprovePost(int id)
    {
        var post = await context.Posts.Include(p => p.Author).FirstOrDefaultAsync(p => p.Id == id);
        if (post == null)
            return NotFound();

        post.Status = PostStatus.Approved;
        await context.SaveChangesAsync();

        try
        {
            await fluentEmail
                .To(post.Author.Email)
                .Subject("Yazınız Onaylandı")
                .Body($"Merhaba {post.Author.UserName},\n\n\"{post.Title}\" başlıklı yazınız moderatör tarafından onaylandı ve yayına alındı.")
                .SendAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApprovePost] Hata: {ex.Message}");
            return StatusCode(500, "Yazı onaylanırken bir hata oluştu.");
        }

        return Ok("Yazı onaylandı ve e-posta gönderildi.");
    }


    [HttpPost("reject-post/{id}")]
    public async Task<IActionResult> RejectPost(int id)
    {
        var post = await context.Posts.Include(p => p.Author).FirstOrDefaultAsync(p => p.Id == id);
        if (post == null)
            return NotFound();

        post.Status = PostStatus.Rejected;
        await context.SaveChangesAsync();

        try
        {
            await fluentEmail
                .To(post.Author.Email)
                .Subject("Yazınız Reddedildi")
                .Body($"Merhaba {post.Author.UserName},\n\n\"{post.Title}\" başlıklı yazınız moderatör tarafından uygun bulunmadığı için reddedildi.")
                .SendAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RejectPost] Hata: {ex.Message}");
            return StatusCode(500, "Yazı reddedilirken bir hata oluştu.");
        }

        return Ok("Yazı reddedildi ve e-posta gönderildi.");
    }


    [HttpGet("comments")]
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


    [HttpDelete("deletecomment/{id}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var comment = await context.Comments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comment == null)
            return NotFound();

        context.Comments.Remove(comment);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
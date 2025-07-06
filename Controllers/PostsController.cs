using System.Net;
using FluentEmail.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniBlog.Data;
using MiniBlog.Models.DTOs;
using MiniBlog.Models.Entities;

namespace MiniBlog.Controllers;
[ApiController]
[Route("[controller]")]
public class PostsController(AppDbContext context,IFluentEmail fluentEmail,UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager):ControllerBase
{
    [HttpGet("Getallposts")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllPosts()
    {
        
        var posts = await context.Posts
            .Include(p=>p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Content,
                Author=p.Author.UserName,
                p.CreatedAt
            })
            .ToListAsync();
            
            if (!posts.Any())
            {
                return NotFound("Yazı Yok");
            }
            return Ok(posts);
           
    }

    [HttpPost("getpost/{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var post = await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .ThenInclude(c => c.Author)
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Content,
                Author = p.Author.UserName,
                p.CreatedAt,
                Comments = p.Comments.Select(c => new
                {
                    c.Id,
                    c.Text,
                    Author = c.Author.UserName,
                    c.CreatedAt
                })
            }).FirstOrDefaultAsync();
        if (post == null)
        {
            return NotFound($"{id} Yazısı Bulunamadı");
        }
        return Ok(post);
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] PostDto post)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        if (string.IsNullOrEmpty(post.Title))
            return BadRequest("Başlık boş olamaz");

        if (string.IsNullOrEmpty(post.Content))
            return BadRequest("İçerik boş olamaz");
        
        var roles = await userManager.GetRolesAsync(user);
        var isModeratorOrAdmin = roles.Contains("Admin") || roles.Contains("Moderator");

        var model = new Post
        {
            Title = post.Title,
            Content = post.Content,
            AuthorId = user.Id,
            CreatedAt = DateTime.UtcNow,
            Status = isModeratorOrAdmin ? PostStatus.Approved : PostStatus.Pending
        };

        context.Posts.Add(model);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = model.Id }, new
        {
            model.Id,
            model.Title,
            model.Content,
            Author = user.UserName,
            model.CreatedAt,
            model.Status
        });
    }


    [HttpPut("update/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] Post updatePost)
    {
       var post =await context.Posts.FindAsync(id);
       if (post is null)
       {
           return NotFound("Yazı Bulunamadı");
       }
       var user = await userManager.GetUserAsync(User);
       if (user == null || user.Id != post.AuthorId)
       {
           return Forbid("Bu Yazıyı Güncelleme Yetkiniz Yok");
       }
       post.Title = updatePost.Title;
       post.Content = updatePost.Content;
       context.Posts.Update(post);
       await context.SaveChangesAsync();
       return Ok(post);
           
    }
    [HttpDelete("delete/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(int id)
    {
        var post = await context.Posts
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound("Yazı bulunamadı.");

        var user = await userManager.GetUserAsync(User);
        if (user == null || user.Id != post.AuthorId)
            return Forbid("Bu yazıyı silme yetkiniz yok.");
        
        context.Comments.RemoveRange(post.Comments);
        context.Posts.Remove(post);
        await context.SaveChangesAsync();

        return NoContent();
    }
    [HttpGet("paged")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPagedPosts(int skip = 0, int take = 10)
    {
        var posts = await context.Posts
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
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
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetApprovedPosts()
    {
        var posts = await context.Posts
            .Include(p => p.Author)
            .Where(p => p.Status == PostStatus.Approved)
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
    
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchPosts([FromQuery] string? q)
    {
        var query = context.Posts
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p => p.Title.Contains(q) || p.Content.Contains(q));
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
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> ApprovePost(int id)
    {
        var post = await context.Posts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            return NotFound("Yazı bulunamadı.");

        post.Status = PostStatus.Approved;
        await context.SaveChangesAsync();

        await fluentEmail
            .To(post.Author.Email)
            .Subject("Yazınız Onaylandı")
            .Body($"Merhaba {post.Author.UserName},\n\n" +
                  $"\"{post.Title}\" başlıklı yazınız moderatör tarafından onaylandı ve yayına alındı.")
            .SendAsync();

        return Ok("Yazı onaylandı ve e-posta gönderildi.");
    }
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> RejectPost(int id)
    {
        var post = await context.Posts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            return NotFound("Yazı bulunamadı.");

        post.Status = PostStatus.Rejected;
        await context.SaveChangesAsync();

        await fluentEmail
            .To(post.Author.Email)
            .Subject("Yazınız Reddedildi")
            .Body($"Merhaba {post.Author.UserName},\n\n" +
                  $"\"{post.Title}\" başlıklı yazınız moderatör tarafından uygun bulunmadığı için reddedildi.")
            .SendAsync();

        return Ok("Yazı reddedildi ve e-posta gönderildi.");
    }
    [HttpGet("pending")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> GetPendingPosts()
    {
        var pendingPosts = await context.Posts
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

        return Ok(pendingPosts);
    }
    [HttpGet("myposts")]
    [Authorize]
    public async Task<IActionResult> GetMyPosts()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var myPosts = await context.Posts
            .Where(p => p.AuthorId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Content,
                p.Status,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(myPosts);
    }
    [HttpGet("rejected")]
    [Authorize]
    public async Task<IActionResult> GetRejectedPosts()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var rejectedPosts = await context.Posts
            .Where(p => p.AuthorId == user.Id && p.Status == PostStatus.Rejected)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Content,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(rejectedPosts);
    }






}
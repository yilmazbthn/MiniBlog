using System.Net;
using FluentEmail.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using MiniBlog.Data;
using MiniBlog.Models.Entities;

namespace MiniBlog.Controllers;
[ApiController]
[Route("aAuth")]
public class AccountController(AppDbContext context,IEmailSender emailSender,SignInManager<IdentityUser> signInManager,IFluentEmail fluentEmail,UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager):ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] Register model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var existingUserByName = await userManager.FindByNameAsync(model.UserName);
        if (existingUserByName != null)
            return BadRequest("Bu kullanıcı adı zaten kullanılıyor.");

        var existingUserByEmail = await userManager.FindByEmailAsync(model.Email);
        if (existingUserByEmail != null)
            return BadRequest("Bu email zaten kullanılıyor.");

        var user = new IdentityUser
        {
            UserName = model.UserName,
            Email = model.Email
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);


        var userRoleExists = await roleManager.RoleExistsAsync("User");
        if (!userRoleExists)
            await roleManager.CreateAsync(new IdentityRole("User"));

        await userManager.AddToRoleAsync(user, "User");


        
        return Ok("Kullanıcı başarıyla kayıt oldu. Lütfen emailinizi doğrulayın.");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] Login model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await userManager.FindByNameAsync(model.Email);
        if (user == null)
            return Unauthorized("Kullanıcı bulunamadı.");

        if (!await userManager.IsEmailConfirmedAsync(user))
            return Unauthorized("Email adresiniz doğrulanmamış.");

        var result = await signInManager.PasswordSignInAsync(user.Email, model.Password, isPersistent: false, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return Ok("Giriş başarılı.");
        }
        else
        {
            return Unauthorized("Kullanıcı adı veya parola yanlış.");
        }
    }
    
}
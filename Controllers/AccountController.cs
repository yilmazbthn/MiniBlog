using FluentEmail.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using MiniBlog.Data;
using MiniBlog.Models.Entities;

namespace MiniBlog.Controllers;

public class AccountController(AppDbContext context,IEmailSender emailSender,IFluentEmail fluentEmail,UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager):ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] Register model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new IdentityUser { UserName = model.UserName, Email = model.Email };
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        
        await userManager.AddToRoleAsync(user, "User");

        return Ok("Kullanıcı başarıyla kayıt oldu ve User rolü atandı.");
    }
}

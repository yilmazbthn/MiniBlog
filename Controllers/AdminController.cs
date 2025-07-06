using FluentEmail.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using MiniBlog.Data;

namespace MiniBlog.Controllers;
[ApiController]
[Route("/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(AppDbContext context,IEmailSender emailSender,IFluentEmail fluentEmail,UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager):ControllerBase
{
    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = userManager.Users.ToList();
        var userList = new List<object>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userList.Add(new
            {
                user.Id,
                user.UserName,
                user.Email,
                Roles = roles
            });
        }
        return Ok(userList);
    }
    [HttpPost("assign-role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType( StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignRole([FromQuery] string userName, [FromQuery] string role)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
                return NotFound($"Kullanıcı '{userName}' bulunamadı.");

            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                    return BadRequest(roleResult.Errors);
            }

            var isInRole = await userManager.IsInRoleAsync(user, role);
            if (isInRole)
                return BadRequest($"Kullanıcı zaten '{role}' rolünde.");

            var result = await userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok($"'{userName}' kullanıcısına '{role}' rolü atandı.");
        }
        
        [HttpPost("remove-role")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveRole([FromQuery] string userName, [FromQuery] string role)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
                return NotFound($"Kullanıcı '{userName}' bulunamadı.");

            var isInRole = await userManager.IsInRoleAsync(user, role);
            if (!isInRole)
                return BadRequest($"Kullanıcı '{role}' rolünde değil.");

            var result = await userManager.RemoveFromRoleAsync(user, role);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok($"'{userName}' kullanıcısından '{role}' rolü kaldırıldı.");
        }
        
        [HttpGet("user-roles/{userName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserRoles(string userName)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
                return NotFound($"Kullanıcı '{userName}' bulunamadı.");

            var roles = await userManager.GetRolesAsync(user);
            return Ok(new { User = userName, Roles = roles });
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult OnlyAdmin() => Ok("Sadece admin görebilir.");

}
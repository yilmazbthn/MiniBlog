
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using MiniBlog;
using MiniBlog.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<IdentityUser>(opt =>
    {
        opt.Password.RequiredLength = 1;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireLowercase = false;
        opt.Password.RequiredUniqueChars = 0;
        opt.Password.RequireDigit = false;
        opt.Password.RequiredUniqueChars = 0;
        opt.SignIn.RequireConfirmedEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/api/Auth/login"; 
    options.AccessDeniedPath = "/api/Auth/denied"; 
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15); 
    options.SlidingExpiration = true; 
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None; 
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS zorunlu
});


builder.Services.Configure<SecurityStampValidatorOptions>(opt => opt.ValidationInterval = TimeSpan.Zero);

builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

var smtpSettings = builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
builder.Services
    .AddFluentEmail(smtpSettings.FromEmail, smtpSettings.FromName)
    .AddRazorRenderer()
    .AddSmtpSender(new SmtpClient(smtpSettings.Host, smtpSettings.Port)
    {
        EnableSsl = true,
        Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password),
    });

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
    {
        policy
            
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedRolesAsync(roleManager);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGroup("Auth").MapIdentityApi<IdentityUser>().WithTags("Auth");


app.Run();

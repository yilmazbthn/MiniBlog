using FluentEmail.Core;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace MiniBlog;

public class EmailSender(IFluentEmailFactory fluentEmail) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        await fluentEmail.Create()
            .To(email)
            .Subject(subject)
            .Body(htmlMessage, true)
            .SendAsync();
    }
}

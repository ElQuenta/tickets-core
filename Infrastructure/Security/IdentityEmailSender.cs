using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using tickets_core.Infrastructure.Identity;

namespace tickets_core.Infrastructure.Security;

public sealed class IdentityEmailSender(IOptions<SmtpOptions> options) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, "Confirma tu correo", $"<p><a href=\"{HtmlEncoder.Default.Encode(confirmationLink)}\">Confirma tu correo</a></p>");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "Restablece tu contraseña", $"<p><a href=\"{HtmlEncoder.Default.Encode(resetLink)}\">Restablece tu contraseña</a></p>");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, "Restablece tu contraseña", $"<p>Tu código para restablecer la contraseña es: <strong>{HtmlEncoder.Default.Encode(resetCode)}</strong></p>");

    private async Task SendAsync(string email, string subject, string body)
    {
        var smtp = options.Value;
        if (string.IsNullOrWhiteSpace(smtp.Host) || string.IsNullOrWhiteSpace(smtp.FromAddress))
            throw new InvalidOperationException("Configura Smtp__Host y Smtp__FromAddress para enviar correos de Identity.");

        using var message = new MailMessage
        {
            From = new MailAddress(smtp.FromAddress, smtp.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(email);
        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.EnableSsl,
            UseDefaultCredentials = false
        };
        if (!string.IsNullOrWhiteSpace(smtp.UserName))
            client.Credentials = new NetworkCredential(smtp.UserName, smtp.Password);
        await client.SendMailAsync(message);
    }
}

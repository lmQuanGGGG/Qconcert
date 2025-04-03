using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;

public class EmailService
{
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress("Qconcert", "your-email@example.com")); // Địa chỉ email của bạn
        email.To.Add(new MailboxAddress("", toEmail));
        email.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = body
        };

        email.Body = builder.ToMessageBody();

        using (var smtp = new SmtpClient())
        {
            await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls); // SMTP server
            await smtp.AuthenticateAsync("your-email@example.com", "your-email-password"); // Email và mật khẩu
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
namespace Qconcert.Service
{
    public class EmailService
    {
    }
}

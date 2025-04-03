using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;
using Qconcert.Services;
using System.Net;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);

// Thêm các dịch vụ vào container
builder.Services.AddControllersWithViews();
builder.Services.AddSession(); // Thêm dòng này
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký TicketBoxDbContext với dependency injection
builder.Services.AddDbContext<TicketBoxDb1Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TicketBoxDb1Context")));

// Đăng ký các dịch TicketService
builder.Services.AddScoped<TicketService>();
// ĐĂng ký dịch vụ địa chỉ

// Cấu hình Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true; // Bắt buộc xác thực email
})
    .AddDefaultTokenProviders()
    .AddDefaultUI()
    .AddEntityFrameworkStores<TicketBoxDb1Context>();

// Cấu hình dịch vụ gửi email
builder.Services.AddSingleton<IEmailSender, EmailSender>();

builder.Services.AddRazorPages();

var app = builder.Build();

// Cấu hình pipeline xử lý HTTP request
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=AdminHome}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");




app.MapRazorPages();

app.Run();

// Lớp gửi email
public class EmailSender : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var smtpClient = new SmtpClient("smtp.gmail.com") 
        {
            Port = 587,
            Credentials = new NetworkCredential("lmquang2004@gmail.com", "ecbw jcdo bfbb gegp"),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress("lmquang2004@gmail.com"),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true,
        };
        // Thêm phần văn bản thuần túy để tránh bị đánh dấu là thư rác
        var plainTextMessage = "Đây là phiên bản văn bản thuần của email. Vui lòng xem phiên bản HTML để có định dạng tốt hơn.";
        var alternateViewPlainText = AlternateView.CreateAlternateViewFromString(plainTextMessage, null, "text/plain");
        var alternateViewHtml = AlternateView.CreateAlternateViewFromString(htmlMessage, null, "text/html");



        mailMessage.To.Add(email);
        await smtpClient.SendMailAsync(mailMessage);
    }
}

using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;
using Qconcert.Services;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Qconcert.Controllers;
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

// Thêm dòng này để bật log ra console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();


// Thêm dịch vụ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("https://pay.payos.vn", "http://localhost:7293") // Thay bằng domain của bạn
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Đăng ký TicketBoxDbContext với dependency injection
builder.Services.AddDbContext<TicketBoxDb1Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TicketBoxDb1Context")));
// Cấu hình Hangfire với SQL Server
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("TicketBoxDb1Context")));

// Thêm Hangfire Server
builder.Services.AddHangfireServer();


// Đăng ký các dịch TicketService
builder.Services.AddScoped<TicketService>();
// ĐĂng ký dịch vụ địa chỉ
builder.Services.AddHttpClient<PayOSService>();

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

// Cấu hình dịch vụ VietQR
builder.Services.AddScoped<VietQRService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddRazorPages();
// Cấu hình dịch vụ PDF

var app = builder.Build();
// Đăng ký job Hangfire sau khi ứng dụng được khởi tạo
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<HomeController>(
        "CheckPendingPromotions",
        controller => controller.CheckAndActivatePendingPromotions(),
        "*/5 * * * *" // Chạy mỗi 5 phút
    );
}
// Cấu hình pipeline xử lý HTTP request
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseHangfireDashboard();


app.UseRouting();
app.UseCors("AllowSpecificOrigins");
app.UseHangfireDashboard();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=AdminHome}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "employee",
    pattern: "{area:exists}/{controller=Scan}/{action=ScanQrCode}/{id?}");



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


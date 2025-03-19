using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;


var builder = WebApplication.CreateBuilder(args);

// Thêm các dịch vụ vào container.
builder.Services.AddControllersWithViews();

// Đăng ký TicketBoxDbContext với dependency injection
builder.Services.AddDbContext<TicketBoxDb1Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TicketBoxDb1Context")));

// Thêm các dịch vụ Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<TicketBoxDb1Context>();

var app = builder.Build();

// Cấu hình pipeline xử lý HTTP request.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Thêm dòng này để map Razor Pages

app.Run();

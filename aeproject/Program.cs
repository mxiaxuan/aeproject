using aeproject.Data;
using aeproject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 添加服務到容器。
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 註冊 AespadbContext
builder.Services.AddDbContext<AespadbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("aespadb"));
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// 配置 HTTP 請求管道。
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAllOrigins");

app.UseAuthorization();

// 確保這裡在 UseRouting 和 UseAuthorization 之間
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // 映射到 API 控制器
    endpoints.MapDefaultControllerRoute(); // 映射到傳統 MVC 控制器
    endpoints.MapRazorPages(); // 支持 Razor 頁面
});

app.Run();

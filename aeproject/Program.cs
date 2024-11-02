using aeproject.Data;
using aeproject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// 添加服務到容器。
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
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

// 設定 Cookie 認證
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Line"; // 設定 LINE 為挑戰方案
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login"; // 當未登入時跳轉到登入頁面
})
.AddOAuth("Line", options =>
{
    options.ClientId = "2006525664"; // 替換為您的 LINE Client ID
    options.ClientSecret = "5bd5d69b01beb519c817bc2e4646d6b2"; // 替換為您的 LINE Client Secret
    options.CallbackPath = new PathString("/auth/line/callback");

    options.AuthorizationEndpoint = "https://access.line.me/oauth2/v2.1/authorize";
    options.TokenEndpoint = "https://api.line.me/oauth2/v2.1/token";
    options.UserInformationEndpoint = "https://api.line.me/v2/profile";

    options.SaveTokens = true; // 保存 token

    options.Scope.Add("profile"); // 請求用戶資料的範圍
    options.Scope.Add("openid"); // 請求 openid

    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "userId");
    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "displayName");

    options.Events = new OAuthEvents
    {
        OnCreatingTicket = async context =>
        {
            var userInfo = context.User;

            // 調試日誌
            Console.WriteLine(userInfo.ToString());

            string userId = string.Empty;
            string displayName = string.Empty;

            try
            {
                userId = userInfo.GetProperty("userId").GetString();
                displayName = userInfo.GetProperty("displayName").GetString();
            }
            catch (KeyNotFoundException)
            {
                // 錯誤處理
            }

            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
            context.Identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
            // 添加其他需要的 claims
        }
    };
});

// 添加 Session 支持
builder.Services.AddSession();

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

// 啟用身份驗證和會話
app.UseAuthentication(); // 這行必須在 UseAuthorization 前面
app.UseAuthorization();
app.UseSession(); // 啟用 Session 支持

// 設定路由
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // 映射到 API 控制器

    // 將特定路由放在 MapDefaultControllerRoute 之前
    endpoints.MapControllerRoute(
        name: "register",
        pattern: "Account/Register",
        defaults: new { controller = "Account", action = "Register" }
    );

    endpoints.MapControllerRoute(
        name: "line-login",
        pattern: "auth/line/login",
        defaults: new { controller = "Auth", action = "LineLogin" }
    );

    endpoints.MapControllerRoute(
        name: "line-callback",
        pattern: "auth/line/callback",
        defaults: new { controller = "Auth", action = "LineCallback" }
    );

    endpoints.MapDefaultControllerRoute(); // 映射到傳統 MVC 控制器
    endpoints.MapRazorPages(); // 支持 Razor 頁面
});

app.Run();
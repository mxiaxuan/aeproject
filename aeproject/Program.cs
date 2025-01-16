using aeproject.Data;
using aeproject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// �K�[�A�Ȩ�e���C
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(Environment.GetEnvironmentVariable("DEFAULT_CONNECTION")));

// ���U AespadbContext
builder.Services.AddDbContext<AespadbContext>(options =>
    options.UseSqlServer(Environment.GetEnvironmentVariable("AESPADB_CONNECTION")));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// ��X���{�Ұt�m
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
})
.AddGoogle(options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
    options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
    options.CallbackPath = "/signin-google";

    options.Events = new OAuthEvents
    {
        OnCreatingTicket = async context =>
        {
            var identity = context.Principal.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var emailClaim = identity.FindFirst(ClaimTypes.Email);
                var nameClaim = identity.FindFirst(ClaimTypes.Name);
                identity.AddClaim(new Claim("GoogleId", context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value));
                Console.WriteLine($"Google user logged in: {emailClaim?.Value}");
            }
        }
    };
})
.AddOAuth("Line", options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("LINE_CLIENT_ID");
    options.ClientSecret = Environment.GetEnvironmentVariable("LINE_CLIENT_SECRET");
    options.CallbackPath = "/auth/line/callback";

    options.AuthorizationEndpoint = "https://access.line.me/oauth2/v2.1/authorize";
    options.TokenEndpoint = "https://api.line.me/oauth2/v2.1/token";
    options.UserInformationEndpoint = "https://api.line.me/v2/profile";

    options.Scope.Clear();
    options.Scope.Add("profile");
    options.Scope.Add("openid");
    options.Scope.Add("email");

    options.Events = new OAuthEvents
    {
        OnCreatingTicket = async context =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, options.UserInformationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
            var response = await context.Backchannel.SendAsync(request);
            var user = await response.Content.ReadFromJsonAsync<JsonElement>();

            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.GetString("userId")));
            context.Identity.AddClaim(new Claim(ClaimTypes.Name, user.GetString("displayName")));
        }
    };
});

// �K�[ Session ���
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

// �t�m HTTP �ШD�޹D�C
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

// �ҥΨ������ҩM�|��
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// �]�w����
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();

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

    endpoints.MapControllerRoute(
        name: "google-login",
        pattern: "auth/google/login",
        defaults: new { controller = "Auth", action = "GoogleLogin" }
    );

    endpoints.MapControllerRoute(
        name: "google-callback",
        pattern: "auth/google/callback",
        defaults: new { controller = "Auth", action = "GoogleCallback" }
    );

    // �K�[�o�ӹw�]���Ѧb��L�S�w���Ѥ���
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
    name: "checkout",
    pattern: "Checkout/{action=Index}/{id?}",
    defaults: new { controller = "Checkout" });

    endpoints.MapRazorPages();
});

app.Run();
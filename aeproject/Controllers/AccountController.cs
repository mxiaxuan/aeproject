//1
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using System.Linq;
using aeproject.Models; // 假設 User 模型在此命名空間
using aeproject.Data;   // 假設 AespadbContext 在此命名空間
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic; // 添加引用以使用 List
using System.Net.Http; // 引入 HttpClient
using System.Net.Http.Headers; // 引入 HttpHeaders
using System.Text.Json; // 引入 Json 序列化

public class AccountController : Controller
{
    private readonly AespadbContext _context;

    public AccountController(AespadbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == username);

        if (user != null && user.PasswordHash == password)
        {
            // 建立認證票據
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // 加入 ID 的 Claim
        };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // 設置 Cookie 登入屬性
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // 更新最後一次登入時間
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        ViewBag.LoginFailed = "無效的帳號或密碼";
        return View();
    }



    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home"); // 導向首頁或指定頁面
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string username, string email, string password, string phone, string firstName, string lastName, DateTime dateOfBirth, string gender, string address)
    {
        if (ModelState.IsValid)
        {
            var existingUser = await _context.Users
                .SingleOrDefaultAsync(u => u.Username == username || u.Email == email);

            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "用戶名或電子郵件已被使用");
                return View();
            }

            var user = new User
            {
                Username = username,
                Email = email,
                Phone = phone,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = DateOnly.FromDateTime(dateOfBirth),
                Gender = gender,
                Address = address,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = true,
                PasswordHash = password
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 註冊成功後，自動登入
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // 加入 ID 的 Claim
        };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpGet]
    [Route("Account/LineLogin")]
    public IActionResult LineLogin(string returnUrl = "/")
    {
        var redirectUrl = Url.Action("LineCallback", "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, "Line");
    }

    [HttpGet]
    [Route("auth/line/callback")]
    public async Task<IActionResult> LineCallback(string code, string returnUrl = "/")
    {
        var accessToken = await GetAccessToken(code);
        var userInfo = await GetUserInfo(accessToken);

        if (userInfo != null)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == userInfo.Email);
            if (user == null)
            {
                user = new User
                {
                    Username = userInfo.DisplayName,
                    Email = userInfo.Email,
                    PasswordHash = string.Empty
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // 登入用戶
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // 加入 ID 的 Claim
        };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Login");
    }

    [HttpGet]
    [Route("Account/LoginWithLine")]
    public IActionResult LoginWithLine(string returnUrl = "/")
    {
        return RedirectToAction("LineLogin", new { returnUrl });
    }

    private async Task<string> GetAccessToken(string code)
    {
        using (var client = new HttpClient())
        {
            var values = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", "https://localhost:7141/auth/line/callback" },
                { "client_id", "2006525664" }, // 使用您的 LINE Channel ID
                { "client_secret", "5bd5d69b01beb519c817bc2e4646d6b2" } // 使用您的 LINE Channel Secret
            };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://api.line.me/oauth2/v2.1/token", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
                return tokenResponse.AccessToken; // 返回 access token
            }
            return null; // 返回 null 代表獲取失敗
        }
    }

    private async Task<UserInfo> GetUserInfo(string accessToken)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetAsync("https://api.line.me/v2/profile");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UserInfo>(responseContent); // 反序列化為 UserInfo 對象 
            }
            return null; // 返回 null 代表獲取失敗
        }
    }
}

// TokenResponse 類
public class TokenResponse
{
    public string AccessToken { get; set; }
    // 可以添加其他必要的屬性
}

// UserInfo 類
public class UserInfo
{
    public string UserId { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; } // 如果需要用戶電子郵件
    // 可以添加其他必要的屬性
}


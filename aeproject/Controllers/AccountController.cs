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
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
        };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // 設置 Cookie 登入屬性
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // 更新最後一次登入時間
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            // 返回 JSON 響應
            return Json(new
            {
                success = true,
                message = "登入成功！歡迎回來！",
                redirectUrl = Url.Action("Index", "Home")
            });
        }

        // 返回失敗的 JSON 響應
        return Json(new
        {
            success = false,
            message = "無效的帳號或密碼！"
        });
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
    public async Task<IActionResult> Register(string username, string email, string password,
    string phone, string firstName, string lastName, DateTime dateOfBirth, string gender, string address)
    {
        try
        {
            // 1. 先檢查 ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                foreach (var error in errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                return View();
            }

            // 2. 檢查用戶是否已存在
            var existingUser = await _context.Users
                .SingleOrDefaultAsync(u => u.Username == username || u.Email == email);

            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "用戶名或電子郵件已被使用");
                return View();
            }

            // 3. 創建新用戶
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

            // 4. 保存用戶
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 5. 建立認證票據並登入
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // 6. 重定向到首頁
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            // 記錄錯誤
            Console.WriteLine($"Registration error: {ex.Message}");
            ModelState.AddModelError(string.Empty, "註冊過程中發生錯誤，請稍後再試");
            return View();
        }
    }

    // Google 登入
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action("GoogleCallback")
        };
        return Challenge(properties, "Google");
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback()
    {
        try
        {
            var result = await HttpContext.AuthenticateAsync();
            if (!result.Succeeded)
            {
                return RedirectToAction("Login");
            }

            // 獲取 Google 用戶資料
            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);
            var googleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

            // 檢查用戶是否已存在
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // 如果用戶不存在，創建新用戶
                user = new User
                {
                    Username = email, // 或使用 name
                    Email = email,
                    FirstName = name,
                    LastName = "", // 可以留空或從 Google 資料中解析
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = true,
                    PasswordHash = "", // Google 登入不需要密碼
                                       // 設置其他必要欄位
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // 建立認證票據
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim("GoogleId", googleId)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // 設置 Cookie
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // 更新最後登入時間
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            // 記錄錯誤
            Console.WriteLine($"Error in GoogleCallback: {ex.Message}");
            return RedirectToAction("Login");
        }
    }

    // Line 登入
    [HttpGet]
    [Route("Account/LineLogin")]
    public IActionResult LineLogin(string returnUrl = "/")
    {
        var redirectUrl = Url.Action("LineCallback", "Account", null, Request.Scheme);

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl,
            Items =
            {
                { "returnUrl", returnUrl }
            }
        };

        return Challenge(properties, "Line");
    }

    [HttpGet]
    [Route("auth/line/callback")]
    public async Task<IActionResult> LineCallback()
    {
        try
        {
            var authenticateResult = await HttpContext.AuthenticateAsync("Line");
            if (!authenticateResult.Succeeded)
            {
                return RedirectToAction("Login");
            }

            var lineIdClaim = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier);
            var displayNameClaim = authenticateResult.Principal.FindFirst(ClaimTypes.Name);
            var emailClaim = authenticateResult.Principal.FindFirst(ClaimTypes.Email);

            if (lineIdClaim == null || displayNameClaim == null)
            {
                return RedirectToAction("Login");
            }

            User user;
            if (emailClaim != null)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim.Value);
            }
            else
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Username == displayNameClaim.Value);
            }

            if (user == null)
            {
                user = new User
                {
                    Username = displayNameClaim.Value,
                    Email = emailClaim?.Value ?? $"{lineIdClaim.Value}@line.com",
                    PasswordHash = string.Empty,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = true
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim("LineId", lineIdClaim.Value)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return LocalRedirect("/");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Line callback error: {ex.Message}");
            return RedirectToAction("Login");
        }
    }

    [HttpGet]
    [Route("Account/LoginWithLine")]
    public IActionResult LoginWithLine(string returnUrl = "/")
    {
        return RedirectToAction("LineLogin", new { returnUrl });
    }

    // TokenResponse 類型，用來處理 LINE OAuth 回應
    public class TokenResponse
    {
        public string AccessToken { get; set; }
    }

    // 用戶資料
    public class UserInfo
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string PictureUrl { get; set; }
        public string StatusMessage { get; set; }
    }
}

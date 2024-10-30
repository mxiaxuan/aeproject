using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using System.Linq;
using aeproject.Models; // 假設 User 模型在此命名空間
using aeproject.Data;   // 假設 AespadbContext 在此命名空間
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

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
        // 根據用戶名查找用戶
        var user = _context.Users.SingleOrDefault(u => u.Username == username);

        if (user != null && VerifyPassword(password, user.PasswordHash))
        {
            // 建立認證票據
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("UserId", user.UserId.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // 設置 Cookie 登入屬性
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // 使 Cookie 持久化
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14), // 可選的到期時間
                RedirectUri = "/Home/Index" // 可選的重定向 URI
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // 更新最後一次登入時間
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "無效的帳號或密碼");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home"); // 導向首頁或指定頁面
    }

    // 驗證密碼方法
    private bool VerifyPassword(string enteredPassword, string storedHash)
    {
        // 分離鹽和哈希值
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var hash = parts[1];

        // 使用相同的鹽對輸入密碼進行哈希
        var hashedInput = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: enteredPassword,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 32));

        return hashedInput == hash; // 比較哈希值
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string username, string email, string password)
    {
        if (ModelState.IsValid)
        {
            // 確保用戶名稱和電子郵件的唯一性
            var existingUser = await _context.Users
                .SingleOrDefaultAsync(u => u.Username == username || u.Email == email);

            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "用戶名或電子郵件已被使用");
                return View();
            }

            // 創建新用戶
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.Now,
                IsActive = true,
                IsAdmin = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 註冊成功後，自動登入
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("UserId", user.UserId.ToString())
            }, CookieAuthenticationDefaults.AuthenticationScheme)));

            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    // 哈希密碼方法
    private string HashPassword(string password)
    {
        byte[] salt = new byte[16]; // 16 位元組的隨機鹽
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 32));

        return $"{Convert.ToBase64String(salt)}.{hashed}"; // 儲存鹽和哈希值
    }
}

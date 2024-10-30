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

            // 設置 Cookie 登入屬性，包括 SameSite
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // 使 Cookie 持久化
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14), // 可選的到期時間
                // 你可以根據需要設定下列屬性
                RedirectUri = "/Home/Index" // 可選的重定向 URI
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // 更新最後一次登入時間
            user.LastLogin = DateTime.Now;
            _context.SaveChanges();

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

    // 驗證密碼方法：此處使用簡單的明文對比，可根據需求換成更安全的密碼雜湊函數
    private bool VerifyPassword(string enteredPassword, string storedHash)
    {
        // 這裡可以用更安全的方式進行密碼驗證
        return enteredPassword == storedHash;
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
            // 確保用戶名稱和電子郵件的唯一性
            var existingUser = await _context.Users
                .SingleOrDefaultAsync(u => u.Username == username || u.Email == email);

            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "用戶名或電子郵件已被使用");
                return View();
            }

            // 創建新用戶，並將 DateTime 轉換為 DateOnly
            var user = new User
            {
                Username = username,
                Email = email,
                Phone = phone,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = DateOnly.FromDateTime(dateOfBirth), // DateTime 轉換為 DateOnly
                Gender = gender,
                Address = address,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = true,
                PasswordHash = HashPassword(password) // 使用哈希密碼
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 註冊成功後，自動登入
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("UserId", user.UserId.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

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

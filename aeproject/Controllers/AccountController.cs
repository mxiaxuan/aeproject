using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using System.Linq;
using aeproject.Models; // 假設 User 模型在此命名空間
using aeproject.Data;   // 假設 UserDbContext 在此命名空間
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Text;

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
}

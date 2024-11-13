using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using aeproject.Models;
using System.Security.Claims;

public class UserFavoriteController : Controller
{
    private readonly AespadbContext _context;

    public UserFavoriteController(AespadbContext context)
    {
        _context = context;
    }

    // 1. 顯示使用者的收藏頁面
    public async Task<IActionResult> Favorites()
    {
        var userId = GetCurrentUserId();  // 獲取當前登入使用者的ID
        var favorites = await _context.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Product)  // 獲取商品資料
            .ToListAsync();

        return View(favorites);
    }

    // 2. 將商品加入收藏
    [HttpPost]
    public async Task<IActionResult> AddToFavorites(int productId)
    {
        var userId = GetCurrentUserId();  // 獲取當前登入使用者的ID

        // 檢查是否已經收藏過該商品
        var existingFavorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

        if (existingFavorite == null)
        {
            var favorite = new Favorite
            {
                UserId = userId,
                ProductId = productId
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Favorites");  // 重定向到收藏頁面
    }

    // 3. 從收藏中移除商品
    [HttpPost]
    public async Task<IActionResult> RemoveFromFavorites(int productId)
    {
        var userId = GetCurrentUserId();  // 獲取當前登入使用者的ID

        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

        if (favorite != null)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Favorites");  // 重定向到收藏頁面
    }

    // 取得當前登入使用者的ID
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }

        throw new Exception("使用者未登入");
    }
}

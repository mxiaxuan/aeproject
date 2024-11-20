using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using aeproject.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

public class UserFavoriteController : Controller
{
    private readonly AespadbContext _context;

    // 直接使用 AespadbContext 來獲取用戶資料
    public UserFavoriteController(AespadbContext context)
    {
        _context = context;
    }

    // 1. 顯示使用者的收藏頁面
    public async Task<IActionResult> Favorites()
    {
        var userId = await GetCurrentUserId();  // 獲取當前登入使用者的ID
        var favorites = await _context.Favorites
            .Where(f => f.UserId == userId)  // 根據 UserId 查找收藏的商品
            .Include(f => f.Product)  // 獲取商品資料
            .ToListAsync();

        return View(favorites);
    }

    // 2. 將商品加入收藏
    [HttpPost]
    public async Task<IActionResult> AddToFavorites(int productId)
    {
        try
        {
            var userId = await GetCurrentUserId();

            // 檢查是否已經收藏過
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

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // 3. 從收藏中移除商品
    [HttpPost]
    public async Task<IActionResult> RemoveFromFavorites(int productId)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Unauthorized();
        }

        try
        {
            var userId = await GetCurrentUserId();

            // 從資料庫中刪除收藏記錄
            var favorite = await _context.Favorites  // 改用 Favorites 而不是 UserFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);  // 改用 Favorites
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // 取得當前登入使用者的ID
    private async Task<int> GetCurrentUserId()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            throw new Exception("請先登入");
        }

        // 假設您在登入時將用戶ID存儲在Claim中
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            throw new Exception("無法獲取用戶ID");
        }

        // 嘗試解析用戶ID
        if (int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }

        throw new Exception("無效的用戶ID格式");
    }
}
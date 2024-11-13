using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using aeproject.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        var userId = await GetCurrentUserId();  // 獲取當前登入使用者的ID

        // 檢查是否已經收藏過該商品
        var existingFavorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

        if (existingFavorite == null)
        {
            var favorite = new Favorite
            {
                UserId = userId,  // 使用當前登入使用者的 ID
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
        var userId = await GetCurrentUserId();  // 獲取當前登入使用者的ID

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
    private async Task<int> GetCurrentUserId()
    {
        // 嘗試將 User.Identity.Name 轉換為整數，這裡假設 User.Identity.Name 是一個存儲使用者 ID 的字串
        if (int.TryParse(User.Identity.Name, out int userId))
        {
            return userId;  // 如果轉換成功，返回 userId
        }
        else
        {
            // 如果轉換失敗，則拋出異常或根據需求處理
            throw new Exception("無效的使用者 ID");
        }
    }
}

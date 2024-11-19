using aeproject.Data;
using aeproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class CartController : Controller
{
    private readonly AespadbContext _dbContext;

    public CartController(AespadbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // 顯示用戶的購物車內容
    public IActionResult Index()
    {
        var userIdString = User.Identity.Name; // 假設這是用戶識別方式，請根據實際需求調整
        if (int.TryParse(userIdString, out var userId)) // 嘗試將 userId 轉換為 int
        {
            var cartItems = _dbContext.Carts
                .Where(c => c.UserId == userId) // 使用整數型別進行比較
                .Include(c => c.Product) // 加入商品資料
                .ToList();

            return View("cart", cartItems);
        }
        else
        {
            // 如果無法轉換為整數，可能是登入的用戶名稱不正確，或者未登入，這裡可以做錯誤處理
            return RedirectToAction("Login", "Account"); // 例如，跳轉到登入頁面
        }
    }

    [HttpPost]
    public IActionResult AddToCart(int productId, int quantity)
    {
        var userIdString = User.Identity.Name; // 獲取當前用戶的ID（為字串類型）

        // 嘗試將 userIdString 轉換為 int
        if (int.TryParse(userIdString, out var userId))
        {
            var existingItem = _dbContext.Carts
                .FirstOrDefault(c => c.UserId == userId && c.ProductId == productId); // 使用 UserId 和 ProductId

            if (existingItem != null)
            {
                // 如果商品已經在購物車中，增加數量
                existingItem.Quantity += quantity;
                existingItem.UpdatedAt = DateTime.Now; // 更新時間
            }
            else
            {
                // 如果商品不在購物車中，創建一個新的項目
                var newItem = new Cart
                {
                    UserId = userId,  // 使用 UserId
                    ProductId = productId, // 使用 ProductId
                    Quantity = quantity,
                    AddedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _dbContext.Carts.Add(newItem);
            }

            _dbContext.SaveChanges();
            return RedirectToAction("Index"); // 添加完後跳轉回購物車頁面
        }
        else
        {
            // 如果 userId 轉換失敗，可能是用戶未登入或者 id 無效，這裡可以處理錯誤
            return RedirectToAction("Login", "Account"); // 例如跳轉到登入頁面
        }
    }


    [HttpPost]
    public IActionResult UpdateQuantity(int cartId, int quantity)
    {
        var cartItem = _dbContext.Carts.Find(cartId);
        if (cartItem != null)
        {
            cartItem.Quantity = quantity; // 使用 Quantity 而不是 quantity
            cartItem.UpdatedAt = DateTime.Now;
            _dbContext.SaveChanges();
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult RemoveFromCart(int cartId)
    {
        var cartItem = _dbContext.Carts.Find(cartId);
        if (cartItem != null)
        {
            _dbContext.Carts.Remove(cartItem);
            _dbContext.SaveChanges();
        }

        return RedirectToAction("Index");
    }
}

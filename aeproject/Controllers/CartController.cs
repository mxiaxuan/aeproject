using aeproject.Data;
using aeproject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System;

[Authorize]
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
        // 改用 NameIdentifier 來獲取用戶 ID
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdString, out var userId))
        {
            var cartItems = _dbContext.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product) // 確保一併載入 Product 資料
                .ToList();

            return View("cart", cartItems);
        }
        else
        {
            return RedirectToAction("Login", "Account");
        }
    }

    // 將商品加入購物車
    [HttpPost]
    public IActionResult AddToCart(int productId, int quantity)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdString, out var userId))
        {
            // 查找該用戶是否已經有這個商品
            var existingItem = _dbContext.Carts
                .FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);

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
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    AddedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _dbContext.Carts.Add(newItem);
            }

            _dbContext.SaveChanges();
            return RedirectToAction("Index", "Cart"); // 添加完後跳轉回購物車頁面
        }
        else
        {
            return RedirectToAction("Login", "Account"); // 跳轉到登入頁面
        }
    }

    // 更新購物車中商品數量
    [HttpPost]
    public IActionResult UpdateQuantity(int cartId, int quantity)
    {
        var cartItem = _dbContext.Carts.Find(cartId);
        if (cartItem != null)
        {
            // 檢查數量是否合法
            if (quantity > 0 && quantity <= cartItem.Product.StockQuantity)
            {
                cartItem.Quantity = quantity;
                cartItem.UpdatedAt = DateTime.Now;
                _dbContext.SaveChanges();
            }
            else
            {
                // 數量超過庫存，或者小於等於0，這裡可以返回錯誤訊息或提示
                TempData["Error"] = "數量超過庫存或無效！";
            }
        }

        return RedirectToAction("Index");
    }
    public IActionResult RemoveFromCart(int cartId)
    {
        using (var transaction = _dbContext.Database.BeginTransaction())
        {
            try
            {
                // 先刪除關聯的 CartItems
                var relatedItems = _dbContext.CartItems
                    .Where(ci => ci.CartId == cartId)
                    .ToList();

                if (relatedItems.Any())
                {
                    _dbContext.CartItems.RemoveRange(relatedItems);  // 確保這裡使用 CartItems，而不是 Cart_Item
                    _dbContext.SaveChanges();
                }

                // 然後刪除購物車項目
                var cartItem = _dbContext.Carts
                    .FirstOrDefault(c => c.CartId == cartId && c.UserId == GetUserId());

                if (cartItem != null)
                {
                    _dbContext.Carts.Remove(cartItem);
                    _dbContext.SaveChanges();

                    transaction.Commit();
                    return Json(new { success = true, message = "商品已成功從購物車移除" });
                }
                else
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "找不到該商品" });
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Json(new
                {
                    success = false,
                    message = $"發生錯誤：{ex.Message}"
                });
            }
        }
    }


    // 獲取當前用戶的 UserId
    private int GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);  // 根據 NameIdentifier 獲取用戶的 ID
        return int.TryParse(userIdString, out var userId) ? userId : 0;  // 解析 ID 並返回
    }


}

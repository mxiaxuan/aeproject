using aeproject.Data;
using aeproject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

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
                .Include(c => c.Product)
                .ToList();

            return View("cart", cartItems);
        }
        else
        {
            return RedirectToAction("Login", "Account");
        }
    }

    [HttpPost]
    public IActionResult AddToCart(int productId, int quantity)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
            return RedirectToAction("Index", "Cart"); // 添加完後跳轉回購物車頁面
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

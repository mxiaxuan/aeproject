using aeproject.Data;
using aeproject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using System;
using System.Text.Json;

[Authorize]
public class CartController : Controller
{
    private readonly AespadbContext _dbContext;
    private readonly ILogger<CartController> _logger;

    public CartController(AespadbContext dbContext, ILogger<CartController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // 顯示用戶的購物車內容
    public IActionResult Index()
    {
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
            _logger.LogWarning("User ID could not be parsed when accessing cart index");
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
            try
            {
                var existingItem = _dbContext.Carts
                    .FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    existingItem.UpdatedAt = DateTime.Now;
                }
                else
                {
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
                _logger.LogInformation($"Successfully added/updated product {productId} to cart for user {userId}");
                return RedirectToAction("Index", "Cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding product {productId} to cart for user {userId}");
                return RedirectToAction("Index", "Cart");
            }
        }
        else
        {
            _logger.LogWarning("User ID could not be parsed when adding to cart");
            return RedirectToAction("Login", "Account");
        }
    }

    [HttpPost]
    public IActionResult UpdateQuantity(int cartId, int quantity)
    {
        try
        {
            var cartItem = _dbContext.Carts
                .Include(c => c.Product)
                .FirstOrDefault(c => c.CartId == cartId);

            if (cartItem == null)
            {
                _logger.LogWarning($"Cart item {cartId} not found during quantity update");
                return Json(new { success = false, message = "購物車項目不存在" });
            }

            if (quantity <= 0)
            {
                _logger.LogWarning($"Invalid quantity {quantity} requested for cart item {cartId}");
                return Json(new { success = false, message = "商品數量必須大於0" });
            }

            if (quantity > cartItem.Product.StockQuantity)
            {
                _logger.LogWarning($"Requested quantity {quantity} exceeds stock {cartItem.Product.StockQuantity} for cart item {cartId}");
                return Json(new
                {
                    success = false,
                    message = $"庫存不足，目前庫存為 {cartItem.Product.StockQuantity} 件",
                    stockQuantity = cartItem.Product.StockQuantity
                });
            }

            cartItem.Quantity = quantity;
            cartItem.UpdatedAt = DateTime.Now;
            _dbContext.SaveChanges();

            decimal subtotal = cartItem.Quantity * cartItem.Product.Price;
            _logger.LogInformation($"Successfully updated quantity for cart item {cartId}");

            return Json(new
            {
                success = true,
                message = "數量更新成功",
                subtotal = subtotal,
                formattedSubtotal = $"NT$ {subtotal:N0}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating quantity for cart item {cartId}");
            return Json(new { success = false, message = "更新數量時發生錯誤，請稍後再試" });
        }
    }

    public IActionResult RemoveFromCart(int cartId)
    {
        using (var transaction = _dbContext.Database.BeginTransaction())
        {
            try
            {
                var relatedItems = _dbContext.CartItems
                    .Where(ci => ci.CartId == cartId)
                    .ToList();

                if (relatedItems.Any())
                {
                    _dbContext.CartItems.RemoveRange(relatedItems);
                    _dbContext.SaveChanges();
                }

                var cartItem = _dbContext.Carts
                    .FirstOrDefault(c => c.CartId == cartId && c.UserId == GetUserId());

                if (cartItem != null)
                {
                    _dbContext.Carts.Remove(cartItem);
                    _dbContext.SaveChanges();

                    transaction.Commit();
                    _logger.LogInformation($"Successfully removed cart item {cartId}");
                    return Json(new { success = true, message = "商品已成功從購物車移除" });
                }
                else
                {
                    transaction.Rollback();
                    _logger.LogWarning($"Cart item {cartId} not found during removal");
                    return Json(new { success = false, message = "找不到該商品" });
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, $"Error removing cart item {cartId}");
                return Json(new
                {
                    success = false,
                    message = $"發生錯誤：{ex.Message}"
                });
            }
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Checkout(string selectedItems)
    {
        if (string.IsNullOrEmpty(selectedItems))
        {
            return BadRequest("未選擇任何商品。");
        }

        List<int> selectedItemIds;

        try
        {
            // 反序列化 selectedItems 字串為 List<int>
            selectedItemIds = JsonSerializer.Deserialize<List<int>>(selectedItems);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "無法反序列化 selectedItems。");
            return BadRequest("選擇的商品格式無效，請重新選擇。");
        }

        if (selectedItemIds == null || !selectedItemIds.Any())
        {
            return BadRequest("無效的商品選擇資料。");
        }

        // 取得選擇的商品及其相關的購物車項目
        var selectedProducts = _dbContext.CartItems
            .Where(ci => selectedItemIds.Contains(ci.CartId) && ci.Cart.UserId == GetUserId())
            .Include(ci => ci.Product)
            .ToList();

        if (!selectedProducts.Any())
        {
            return BadRequest("無法找到選擇的商品，請確認您選擇的商品是否有效。");
        }

        // 建立新的訂單
        var newOrder = new Order
        {
            UserId = GetUserId(),
            OrderDate = DateTime.Now
        };

        // 儲存訂單並獲取 OrderId
        _dbContext.Orders.Add(newOrder);
        _dbContext.SaveChanges();

        // 根據選擇的商品生成 OrderItem，並將其儲存到資料庫
        var orderItems = selectedProducts.Select(p => new OrderItem
        {
            OrderId = newOrder.OrderId,  // 設置 OrderId，這樣 OrderItem 就會關聯到訂單
            ProductId = p.ProductId,
            Quantity = p.Quantity,
            Price = p.Product.Price,
            TotalPrice = p.Quantity * p.Product.Price
        }).ToList();

        // 儲存 OrderItem
        _dbContext.OrderItems.AddRange(orderItems);
        _dbContext.SaveChanges();

        // 重定向到確認訂單頁面
        var createdOrderId = newOrder.OrderId;
        return RedirectToAction("ConfirmOrder", new { orderId = createdOrderId });
    }

    // 獲取當前用戶的 ID
    private int GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdString, out var userId) ? userId : 0;
    }
}
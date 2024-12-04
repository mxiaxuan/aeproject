using aeproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace aeproject.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly AespadbContext _context;

        public CheckoutController(AespadbContext context)
        {
            _context = context;
        }

        // 處理從購物車選中的商品結帳 和 直接購買
        [HttpPost]
        public IActionResult Index(string selectedItems, int? productId = null, int? quantity = null)
        {
            List<CartItem> cartItems;

            // 如果是直接購買（從商品詳情頁）
            if (productId.HasValue && quantity.HasValue)
            {
                var product = _context.Products.Find(productId.Value);
                if (product == null || product.StockQuantity < quantity.Value)
                {
                    return RedirectToAction("Index", "Cart");
                }

                cartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        ProductId = productId.Value,
                        Product = product,
                        Quantity = quantity.Value
                    }
                };
            }
            // 如果是從購物車結帳
            else if (!string.IsNullOrEmpty(selectedItems))
            {
                List<int> selectedItemIds;
                try
                {
                    // 反序列化選中的商品 ID
                    selectedItemIds = JsonSerializer.Deserialize<List<int>>(selectedItems);
                }
                catch
                {
                    return RedirectToAction("Index", "Cart");
                }

                // 取得選中的購物車項目
                cartItems = _context.Carts
                    .Include(c => c.Product)
                    .Where(c => selectedItemIds.Contains(c.CartId) && c.UserId == GetCurrentUserId())
                    .Select(c => new CartItem
                    {
                        ProductId = c.ProductId,
                        Product = c.Product,
                        Quantity = c.Quantity
                    })
                    .ToList();
            }
            else
            {
                return RedirectToAction("Index", "Cart");
            }

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.CartItems = cartItems;
            ViewBag.TotalAmount = cartItems.Sum(item => item.Total);

            return View("Checkout");
        }

        [HttpGet]
        public IActionResult Index()
        {
            // 這是為了處理直接訪問 /Checkout/Index 的情況
            return View("Checkout");
        }


        [HttpPost]
        public IActionResult SubmitOrder(
            // Shipping Information
            string shippingAddress,
            string contactPhone,

            // Payment Method
            string paymentMethod,

            // Orderer Information
            string ordererName,
            string ordererPhone,
            string ordererAddress,
            DateTime? ordererBirthday,

            // Order Notes
            string orderNotes,

            // Invoice Information
            string invoiceType,
            string vehicleType,
            string phoneBarcodeInput,
            string taxId)
        {
            // 先獲取當前用戶ID
            int currentUserId = GetCurrentUserId();

            // 直接從資料庫獲取購物車項目
            var cartItems = _context.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == currentUserId)
                .Select(c => new CartItem
                {
                    ProductId = c.ProductId,
                    Product = c.Product,
                    Quantity = c.Quantity
                })
                .ToList();

            if (!cartItems.Any())
            {
                ModelState.AddModelError("", "購物車是空的");
                // Redirect to the checkout page after error
                return RedirectToAction("Index", "Checkout");
            }

            // 原有的驗證邏輯和訂單處理代碼
            var totalAmount = cartItems.Sum(item => item.Product.Price * item.Quantity);

            var order = new Order
            {
                UserId = currentUserId,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                OrderStatus = "Pending",
                ShippingAddress = shippingAddress,
                ContactPhone = contactPhone,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 先保存訂單
                    _context.Orders.Add(order);
                    _context.SaveChanges();

                    // 創建訂單項目
                    foreach (var item in cartItems)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.OrderId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Price = item.Product.Price,
                            TotalPrice = item.Product.Price * item.Quantity,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        _context.OrderItems.Add(orderItem);

                        // 更新庫存
                        var product = _context.Products.Find(item.ProductId);
                        if (product != null && product.StockQuantity >= item.Quantity)
                        {
                            product.StockQuantity -= item.Quantity;
                        }
                        else
                        {
                            throw new Exception($"產品 {item.Product.ProductName} 庫存不足");
                        }
                    }

                    // 清除購物車
                    var userCarts = _context.Carts.Where(c => c.UserId == currentUserId);
                    _context.Carts.RemoveRange(userCarts);

                    _context.SaveChanges();
                    transaction.Commit();

                    // Redirect to Confirmation page after successful order
                    return RedirectToAction("Index", "Checkout", new { orderId = order.OrderId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "訂單處理失敗：" + ex.Message);

                    // 重新設置購物車項目以顯示在頁面上
                    ViewBag.CartItems = cartItems;
                    ViewBag.TotalAmount = totalAmount;

                    // Redirect to checkout on failure
                    return RedirectToAction("Checkout", "Checkout");
                }
            }
        }

        // 修改 Confirmation 方法以確保關聯資料被載入
        [HttpGet]
        public IActionResult Confirmation(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View("Confirmation", order);
        }

        // 模擬方法
        private int GetCurrentUserId()
        {
            // 從 ClaimsPrincipal 獲取當前登入用戶的 ID
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }
        private List<CartItem> GetCartItems()
        {
            // Assuming you want to get cart items for the current user's cart
            return _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.Cart.UserId == GetCurrentUserId())
                .ToList();
        }

        private void ClearCart()
        {
            // 刪除當前用戶的所有購物車項目
            var cartItems = _context.CartItems
                .Where(c => c.Cart.UserId == GetCurrentUserId());

            _context.CartItems.RemoveRange(cartItems);
            _context.SaveChanges();
        }
    }
}

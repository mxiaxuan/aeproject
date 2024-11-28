using aeproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aeproject.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly AespadbContext _context;

        public CheckoutController(AespadbContext context)
        {
            _context = context;
        }

        // 顯示結帳頁面
        [HttpPost]
        public IActionResult Index(int productId, int quantity)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == productId);
            if (product == null)
            {
                return NotFound();
            }

            var cartItem = new CartItem
            {
                ProductId = productId,
                Product = product,
                Quantity = quantity
            };

            var cartItems = new List<CartItem> { cartItem };

            ViewBag.CartItems = cartItems;
            ViewBag.TotalAmount = cartItem.Total;

            return View("Checkout");
        }

        [HttpGet]
        public IActionResult Index()
        {
            // 這是為了處理直接訪問 /Checkout/Index 的情況
            return View("Checkout");
        }


        [HttpPost]
        public IActionResult SubmitOrder(string shippingAddress, string contactPhone)
        {
            var cartItems = GetCartItems();

            if (!cartItems.Any())
            {
                ModelState.AddModelError("", "購物車是空的");
                return View("Index");
            }

            if (string.IsNullOrWhiteSpace(shippingAddress))
            {
                ModelState.AddModelError("ShippingAddress", "配送地址為必填");
                return View("Index");
            }

            if (string.IsNullOrWhiteSpace(contactPhone) || !System.Text.RegularExpressions.Regex.IsMatch(contactPhone, @"^09\d{8}$"))
            {
                ModelState.AddModelError("ContactPhone", "請輸入正確的手機號碼");
                return View("Index");
            }

            var totalAmount = cartItems.Sum(item => item.Total);

            var order = new Order
            {
                UserId = GetCurrentUserId(),
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                OrderStatus = "Pending",
                ShippingAddress = shippingAddress,
                ContactPhone = contactPhone,  // 新增聯繫電話
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.Orders.Add(order);
                    _context.SaveChanges();

                    foreach (var item in cartItems)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.OrderId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Price = item.Product.Price,
                            TotalPrice = item.Total,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        _context.OrderItems.Add(orderItem);

                        // 更新產品庫存
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

                    _context.SaveChanges();
                    transaction.Commit();

                    ClearCart();
                    return RedirectToAction("Confirmation", new { orderId = order.OrderId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "訂單處理失敗：" + ex.Message);
                    return View("Index");
                }
            }
        }

        // 訂單確認頁
        [HttpGet]
        public IActionResult Confirmation(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
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

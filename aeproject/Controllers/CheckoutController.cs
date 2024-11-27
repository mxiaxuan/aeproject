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
        [HttpGet]
        public IActionResult Index()
        {
            var cartItems = GetCartItems(); // 從購物車中提取內容
            var totalAmount = cartItems.Sum(item => item.Total); // 使用 Total 屬性計算總金額

            ViewBag.CartItems = cartItems;   // 將購物車商品存入 ViewBag
            ViewBag.TotalAmount = totalAmount; // 總金額存入 ViewBag

            return View("checkout");  // 修改此處為 'checkout' 視圖名稱
        }

        [HttpPost]
        public IActionResult SubmitOrder(string shippingAddress)
        {
            var cartItems = GetCartItems(); // 從購物車中提取內容
            var totalAmount = cartItems.Sum(item => item.Total); // 使用 Total 屬性計算總金額

            if (string.IsNullOrWhiteSpace(shippingAddress))
            {
                ModelState.AddModelError("ShippingAddress", "配送地址為必填");
                ViewBag.CartItems = cartItems;
                ViewBag.TotalAmount = totalAmount;
                return View("checkout");  // 返回 'checkout' 視圖
            }

            // 創建訂單
            var order = new Order
            {
                UserId = GetCurrentUserId(),
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                OrderStatus = "Pending",
                ShippingAddress = shippingAddress,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            // 添加訂單項目
            foreach (var item in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price, // 確保產品的單價可用
                    TotalPrice = item.Total,    // 使用 Total 屬性
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.OrderItems.Add(orderItem);
            }

            _context.SaveChanges();

            // 清空購物車
            ClearCart();

            return RedirectToAction("Confirmation", new { orderId = order.OrderId });
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
        private int GetCurrentUserId() => 1; // 模擬用戶ID，應替換為實際用戶ID
        private List<CartItem> GetCartItems() => new List<CartItem>(); // 模擬提取購物車內容
        private void ClearCart() { } // 模擬清空購物車的方法
    }
}

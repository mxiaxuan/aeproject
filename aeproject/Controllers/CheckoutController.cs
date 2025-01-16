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

        [HttpGet]
        public IActionResult Index(string selectedItems, int? productId = null, int? quantity = null)
        {
            List<CartItem> cartItems;

            // 直接購買商品
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
            // 從購物車結帳
            else if (!string.IsNullOrEmpty(selectedItems))
            {
                List<int> selectedItemIds;
                try
                {
                    selectedItemIds = JsonSerializer.Deserialize<List<int>>(selectedItems);
                }
                catch
                {
                    return RedirectToAction("Index", "Cart");
                }

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
            ViewBag.TotalAmount = cartItems.Sum(item => item.Product.Price * item.Quantity);

            return View("Checkout");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitOrder(
            string shippingAddress,
            string contactPhone,
            string paymentMethod,
            string ordererName,
            string ordererPhone,
            string ordererAddress,
            DateOnly? ordererBirthday,
            string orderNotes,
            string invoiceType,
            string vehicleType,
            string phoneBarcodeInput,
            string taxId)
        {
            try
            {
                // 1. 記錄開始處理
                Console.WriteLine("開始處理訂單...");

                var currentUserId = GetCurrentUserId();
                Console.WriteLine($"當前用戶 ID: {currentUserId}");

                // 2. 檢查購物車
                var cartItems = _context.Carts
                    .Include(c => c.Product)
                    .Where(c => c.UserId == currentUserId)
                    .ToList();

                Console.WriteLine($"購物車項目數量: {cartItems.Count}");

                if (!cartItems.Any())
                {
                    Console.WriteLine("購物車為空");
                    return Json(new { success = false, message = "購物車是空的" });
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // 3. 建立訂單
                        Console.WriteLine("正在建立訂單...");
                        var order = new Order
                        {
                            UserId = currentUserId,
                            OrderDate = DateTime.Now,
                            TotalAmount = cartItems.Sum(item => item.Product.Price * item.Quantity),
                            OrderStatus = "Pending",
                            ShippingAddress = shippingAddress,
                            ContactPhone = contactPhone,
                            Payment_method = paymentMethod,
                            Orderer_name = ordererName,
                            Orderer_phone = ordererPhone,
                            Orderer_address = ordererAddress,
                            Orderer_birthday = ordererBirthday ?? DateOnly.FromDateTime(DateTime.Now),
                            Order_notes = orderNotes,
                            Invoice_type = invoiceType,
                            Vehicle_type = vehicleType,
                            Phone_barcode = phoneBarcodeInput,
                            Tax_id = taxId,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };

                        // 4. 添加訂單到資料庫
                        Console.WriteLine("正在保存訂單到資料庫...");
                        _context.Orders.Add(order);
                        _context.SaveChanges();
                        Console.WriteLine($"訂單已建立，ID: {order.OrderId}");

                        // 5. 建立訂單項目
                        Console.WriteLine("正在建立訂單項目...");
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
                            if (product != null)
                            {
                                Console.WriteLine($"更新商品 {product.ProductId} 庫存，原數量: {product.StockQuantity}，減少: {item.Quantity}");
                                product.StockQuantity -= item.Quantity;
                                _context.Update(product);
                            }
                        }

                        // 6. 清除購物車
                        Console.WriteLine("正在清除購物車...");
                        _context.Carts.RemoveRange(cartItems);

                        // 7. 保存所有更改
                        Console.WriteLine("正在保存所有更改...");
                        _context.SaveChanges();

                        // 8. 提交交易
                        Console.WriteLine("正在提交交易...");
                        transaction.Commit();

                        // 9. 設定 TempData
                        TempData["OrderId"] = order.OrderId;
                        Console.WriteLine("訂單處理完成!");

                        return Json(new
                        {
                            success = true,
                            redirectUrl = Url.Action("OrderConfirmation", "Checkout")
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"交易過程中發生錯誤: {ex.Message}");
                        Console.WriteLine($"錯誤詳情: {ex.InnerException?.Message}");
                        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                        throw; // 重新拋出例外以便外層 catch 捕獲
                    }
                }
            }
            catch (Exception ex
            {
                Console.WriteLine($"訂單處理失敗: {ex.Message}");
                Console.WriteLine($"內部錯誤: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Json(new
                {
                    success = false,
                    message = $"訂單處理失敗: {ex.Message}",
                    details = new
                    {
                        error = ex.Message,
                        innerError = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace
                    }
                });
            }
        }

        [HttpGet]
        public IActionResult OrderConfirmation()
        {
            if (!TempData.ContainsKey("OrderId"))
            {
                return RedirectToAction("Index", "Home");
            }

            int orderId = (int)TempData["OrderId"];
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == GetCurrentUserId());

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View("Confirmation", order);
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }
    }
}
using aeproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aeproject.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AespadbContext _context;

        public ProductsController(AespadbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            return View("Products", products);
        }

        // 商品詳細頁
        public async Task<IActionResult> Details(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // 檢查使用者是否已收藏此商品
            bool isFavorite = false;
            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    isFavorite = await _context.Favorites
                        .AnyAsync(f => f.UserId == userId && f.ProductId == id);
                }
            }

            // 根據 AlbumId 查詢同一專輯的其他商品（排除當前商品）
            var recommendedProducts = _context.Products
                                          .Where(p => p.AlbumId == product.AlbumId && p.ProductId != product.ProductId)
                                          .ToList();

            ViewData["Product"] = product;
            ViewData["RecommendedProducts"] = recommendedProducts;
            ViewData["IsFavorite"] = isFavorite; // 傳遞收藏狀態到視圖

            return View(product);
        }
    }
}
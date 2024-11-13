using aeproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            return View("Products", products); // 指定使用 Products.cshtml 檔案
        }

        // 商品詳細頁
        public IActionResult Details(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // 根據 AlbumId 查詢同一專輯的其他商品（排除當前商品）
            var recommendedProducts = _context.Products
                                              .Where(p => p.AlbumId == product.AlbumId && p.ProductId != product.ProductId)
                                              .ToList();

            // 使用 ViewData 或 ViewBag 傳遞資料
            ViewData["Product"] = product;
            ViewData["RecommendedProducts"] = recommendedProducts;

            return View(product);
        }
    }
}



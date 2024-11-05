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
    }
}



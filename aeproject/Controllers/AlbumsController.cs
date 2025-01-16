using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using aeproject.Models;

namespace aeproject.Controllers
{
    public class AlbumsController : Controller
    {
        private readonly AespadbContext _context;

        public AlbumsController(AespadbContext context)
        {
            _context = context;
        }

        // GET: Albums
        // GET: Partial view for albums list
        public async Task<IActionResult> AlbumListPartial()
        {
            var albums = await _context.Albums
                .OrderBy(a => a.ReleaseDate)// 按發行日期升序排序
                .ToListAsync();

            return PartialView("_AlbumListPartial", albums);
        }


        private bool AlbumExists(int id)
        {
            return _context.Albums.Any(e => e.AlbumId == id);
        }
    }
}

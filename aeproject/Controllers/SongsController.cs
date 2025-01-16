using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using aeproject.Models;
using Microsoft.EntityFrameworkCore;

namespace aeproject.Controllers
{
    public class SongsController : Controller
    {
        private readonly AespadbContext _context; // 使用你的 DbContext 類別名稱

        // 使用依賴注入初始化資料庫上下文
        public SongsController(AespadbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetSongsByAlbumId(int? albumId)
        {
            // 檢查是否有提供專輯 ID
            if (albumId == null)
            {
                return BadRequest("Album ID is required.");
            }

            // 查詢專輯相關的歌曲
            var songs = await _context.Songs
                .Where(s => s.AlbumId == albumId)
                .ToListAsync();

            // 如果沒有找到相關的歌曲，回傳 NotFound
            if (songs == null || !songs.Any())
            {
                return NotFound($"No songs found for album ID {albumId}");
            }

            // 回傳歌曲列表
            return Ok(songs);
        }

    }
}

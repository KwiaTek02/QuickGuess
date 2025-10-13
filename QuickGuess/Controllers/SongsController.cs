using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickGuess.Data;

namespace QuickGuess.Controllers
{
    [ApiController]
    [Route("api/songs")]
    public class SongsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly Random _random = new();

        public SongsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("random")]
        public async Task<IActionResult> GetRandomSongs([FromQuery] int count = 1)
        {
            var all = await _db.Songs.ToListAsync();
            if (!all.Any()) return NotFound("Brak piosenek w bazie.");

            var selected = all.OrderBy(x => _random.Next()).Take(count)
                .Select(x => new { x.Id, x.Title })
                .ToList();

            return Ok(selected);
        }

        [HttpGet("titles")]
        public async Task<IActionResult> GetTitles([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return BadRequest();

            var parts = query
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var q = _db.Songs.AsQueryable();

            foreach (var part in parts)
            {
                var p = part; 
                q = q.Where(s =>
                    EF.Functions.ILike(s.Title, $"%{p}%") ||
                    EF.Functions.ILike(s.Artist, $"%{p}%")
                );
            }

            var titles = await q
                .Select(s => $"{s.Artist} - {s.Title} ({s.ReleaseYear})")
                .Distinct()
                .Take(10)
                .ToListAsync();

            return Ok(titles);
        }

    }
}
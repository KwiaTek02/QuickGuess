using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickGuess.Data;

namespace QuickGuess.Controllers
{
    [ApiController]
    [Route("api/movies")]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly Random _random = new();

        public MoviesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("random")]
        public async Task<IActionResult> GetRandomMovies([FromQuery] int count = 1)
        {
            var all = await _db.Movies.ToListAsync();
            if (!all.Any()) return NotFound("Brak filmów w bazie.");

            var selected = all.OrderBy(x => _random.Next()).Take(count)
                .Select(x => new { x.Id, x.Title })
                .ToList();

            return Ok(selected);
        }

        [HttpGet("titles")]
        public async Task<IActionResult> GetTitles([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return BadRequest();
            var titles = await _db.Movies
                .Where(m => EF.Functions.ILike(m.Title, $"%{query}%"))
                .Select(m => $"{m.Title} ({m.ReleaseYear})")
                .Distinct()
                .Take(10)
                .ToListAsync();

            return Ok(titles);
        }
    }
}
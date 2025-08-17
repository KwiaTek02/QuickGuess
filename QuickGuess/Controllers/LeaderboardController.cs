using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickGuess.Data;
using QuickGuess.Models;


namespace QuickGuess.Controllers
{
    [ApiController]
    [Route("api/leaderboard")]
    public class LeaderboardController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public LeaderboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("global")]
        public async Task<IActionResult> GetGlobalLeaderboard()
        {
            var data = await _db.Leaderboards
                .Include(l => l.User)
                .OrderByDescending(l => l.ScoreTotal)
                .Take(100)
                .Select(l => new { l.User.Username, l.ScoreTotal })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("songs")]
        public async Task<IActionResult> GetSongsLeaderboard()
        {
            var data = await _db.Leaderboards
                .Include(l => l.User)
                .OrderByDescending(l => l.ScoreSongs)
                .Take(100)
                .Select(l => new { l.User.Username, l.ScoreSongs })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("movies")]
        public async Task<IActionResult> GetMoviesLeaderboard()
        {
            var data = await _db.Leaderboards
                .Include(l => l.User)
                .OrderByDescending(l => l.ScoreMovies)
                .Take(100)
                .Select(l => new { l.User.Username, l.ScoreMovies })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("global-all")]
        public async Task<IActionResult> GetGlobalLeaderboardAll()
        {
            var data = await _db.Leaderboards
                .Include(l => l.User)
                .OrderByDescending(l => l.ScoreTotal)
                .Select(l => new {
                    PublicId = l.User.PublicId,
                    Username = l.User.Username,
                    scoreTotal = l.ScoreTotal
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}

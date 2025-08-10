using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickGuess.Data;
using QuickGuess.DTOs.Game;
using System.Security.Claims;

namespace QuickGuess.Controllers
{
    [ApiController]
    [Route("api/stats")]
    [Authorize] // jeśli masz autoryzację
    public class StatsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public StatsController(ApplicationDbContext db) => _db = db;

        private Guid GetUserId()
        {
            // dopasuj do swojej autoryzacji
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");
            return Guid.Parse(sub);
        }

        [HttpGet("me/song-ranking")]
        public async Task<ActionResult<PlayerSongStatsDto>> GetMySongRankingStats()
        {
            var userId = GetUserId();

            var q = _db.Guesses.AsNoTracking()
                .Where(g => g.UserId == userId && g.Type == "song" && g.Mode == "ranking");

            var games = await q.CountAsync();
            var correct = await q.CountAsync(g => g.Correct);
            var incorrect = games - correct;
            var winRate = games > 0 ? (double)correct / games : 0.0;

            // min po nullable int – OK
            var bestTime = await q.Where(g => g.Correct)
                .Select(g => (int?)g.Duration)
                .MinAsync() ?? 0;

            // TU była awaria – używamy nullable Average albo warunkowe Average
            var avgTime = await q.Select(g => (double?)g.Duration).AverageAsync() ?? 0.0;
            // alternatywnie:
            // var avgTime = await q.AnyAsync() ? await q.AverageAsync(g => (double)g.Duration) : 0.0;

            // total score – najpierw spróbuj z leaderboards, fallback do sumy guesses
            var totalScore = await _db.Leaderboards
                                 .Where(l => l.UserId == userId)
                                 .Select(l => (int?)l.ScoreSongs)
                                 .FirstOrDefaultAsync()
                             ?? await q.SumAsync(g => g.Score);

            // bieżący score do rankingu (songs)
            var myScore = await _db.Leaderboards
                .Where(l => l.UserId == userId)
                .Select(l => (int?)l.ScoreSongs)
                .FirstOrDefaultAsync() ?? totalScore;

            var rankingPosition = 0;
            if (myScore > 0)
            {
                rankingPosition = await _db.Leaderboards.CountAsync(l => l.ScoreSongs > myScore) + 1;
            }

            return new PlayerSongStatsDto
            {
                Games = games,
                Correct = correct,
                Incorrect = incorrect,
                WinRate = winRate,
                RankingPosition = Math.Max(rankingPosition, 0),
                BestTimeSec = bestTime,
                AvgTimeSec = avgTime,
                TotalScore = totalScore
            };
        }

    }
}

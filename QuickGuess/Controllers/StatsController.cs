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
    [Authorize]
    public class StatsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public StatsController(ApplicationDbContext db) => _db = db;

        private Guid GetUserId()
        {
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

            var bestMs = await q.Where(g => g.Correct)
                .Select(g => (int?)g.Duration)
                .MinAsync() ?? 0;
            var bestTime = Math.Round(bestMs / 1000.0, 2, MidpointRounding.AwayFromZero);

            var avgMs = await q.Select(g => (double?)g.Duration).AverageAsync() ?? 0.0;
            var avgTime = avgMs / 1000.0;

            var totalScore = await _db.Leaderboards
                                 .Where(l => l.UserId == userId)
                                 .Select(l => (int?)l.ScoreSongs)
                                 .FirstOrDefaultAsync()
                             ?? await q.SumAsync(g => g.Score);

            totalScore = Math.Max(totalScore, 0);

            var myScore = await _db.Leaderboards
                .Where(l => l.UserId == userId)
                .Select(l => (int?)l.ScoreSongs)
                .FirstOrDefaultAsync() ?? totalScore;

            myScore = Math.Max(myScore, 0);


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

        [HttpGet("me/movie-ranking")]
        public async Task<ActionResult<PlayerMovieStatsDto>> GetMyMovieRankingStats()
        {
            var userId = GetUserId();

            var q = _db.Guesses.AsNoTracking()
                .Where(g => g.UserId == userId && g.Type == "movie" && g.Mode == "ranking");

            var games = await q.CountAsync();
            var correct = await q.CountAsync(g => g.Correct);
            var incorrect = games - correct;
            var winRate = games > 0 ? (double)correct / games : 0.0;

            var bestMs = await q.Where(g => g.Correct)
                .Select(g => (int?)g.Duration)
                .MinAsync() ?? 0;
            var bestTime = Math.Round(bestMs / 1000.0, 2, MidpointRounding.AwayFromZero);

            var avgMs = await q.Select(g => (double?)g.Duration).AverageAsync() ?? 0.0;
            var avgTime = avgMs / 1000.0;

            var totalScore = await _db.Leaderboards
                                 .Where(l => l.UserId == userId)
                                 .Select(l => (int?)l.ScoreMovies)
                                 .FirstOrDefaultAsync()
                             ?? await q.SumAsync(g => g.Score);

            totalScore = Math.Max(totalScore, 0);

            var myScore = await _db.Leaderboards
                .Where(l => l.UserId == userId)
                .Select(l => (int?)l.ScoreMovies)
                .FirstOrDefaultAsync() ?? totalScore;

            myScore = Math.Max(myScore, 0);

            var rankingPosition = 0;
            if (myScore > 0)
                rankingPosition = await _db.Leaderboards.CountAsync(l => l.ScoreMovies > myScore) + 1; // jw.

            return new PlayerMovieStatsDto
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

        [HttpGet("user/{publicId:guid}/song-ranking")]
        [AllowAnonymous]
        public async Task<ActionResult<PlayerSongStatsDto>> GetSongRankingFor(Guid publicId)
        {
            var userId = await _db.Users.Where(u => u.PublicId == publicId)
                                        .Select(u => u.Id)
                                        .FirstOrDefaultAsync();
            if (userId == default) return NotFound();

            var q = _db.Guesses.AsNoTracking()
                .Where(g => g.UserId == userId && g.Type == "song" && g.Mode == "ranking");

            var games = await q.CountAsync();
            var correct = await q.CountAsync(g => g.Correct);
            var incorrect = games - correct;
            var winRate = games > 0 ? (double)correct / games : 0.0;
            var bestMs = await q.Where(g => g.Correct).Select(g => (int?)g.Duration).MinAsync() ?? 0;
            var bestTime = (int)Math.Round(bestMs / 1000.0, MidpointRounding.AwayFromZero);

            var avgMs = await q.Select(g => (double?)g.Duration).AverageAsync() ?? 0.0;
            var avgTime = avgMs / 1000.0;

            var totalScore = await _db.Leaderboards
                                 .Where(l => l.UserId == userId)
                                 .Select(l => (int?)l.ScoreSongs)
                                 .FirstOrDefaultAsync()
                             ?? await q.SumAsync(g => g.Score);

            totalScore = Math.Max(totalScore, 0);

            var myScore = await _db.Leaderboards
                .Where(l => l.UserId == userId)
                .Select(l => (int?)l.ScoreSongs)
                .FirstOrDefaultAsync() ?? totalScore;

            myScore = Math.Max(myScore, 0);

            var rankingPosition = myScore > 0
                ? await _db.Leaderboards.CountAsync(l => l.ScoreSongs > myScore) + 1
                : 0;

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


        [HttpGet("user/{publicId:guid}/movie-ranking")]
        [AllowAnonymous]
        public async Task<ActionResult<PlayerMovieStatsDto>> GetMovieRankingFor(Guid publicId)
        {
            var userId = await _db.Users.Where(u => u.PublicId == publicId)
                                        .Select(u => u.Id).FirstOrDefaultAsync();
            if (userId == default) return NotFound();

            var q = _db.Guesses.AsNoTracking()
                .Where(g => g.UserId == userId && g.Type == "movie" && g.Mode == "ranking");

            var games = await q.CountAsync();
            var correct = await q.CountAsync(g => g.Correct);
            var incorrect = games - correct;
            var winRate = games > 0 ? (double)correct / games : 0.0;
            var bestMs = await q.Where(g => g.Correct).Select(g => (int?)g.Duration).MinAsync() ?? 0;
            var bestTime = (int)Math.Round(bestMs / 1000.0, MidpointRounding.AwayFromZero);

            var avgMs = await q.Select(g => (double?)g.Duration).AverageAsync() ?? 0.0;
            var avgTime = avgMs / 1000.0;
            var totalScore = await _db.Leaderboards
                                 .Where(l => l.UserId == userId)
                                 .Select(l => (int?)l.ScoreMovies)
                                 .FirstOrDefaultAsync()
                             ?? await q.SumAsync(g => g.Score);

            totalScore = Math.Max(totalScore, 0);



            var myScore = await _db.Leaderboards
                .Where(l => l.UserId == userId)
                .Select(l => (int?)l.ScoreMovies)
                .FirstOrDefaultAsync() ?? totalScore;

            myScore = Math.Max(myScore, 0);

            var rankingPosition = myScore > 0
                ? await _db.Leaderboards.CountAsync(l => l.ScoreMovies > myScore) + 1
                : 0;

            return new PlayerMovieStatsDto
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

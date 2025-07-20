using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QuickGuess.Data;
using QuickGuess.DTOs.Game;
using QuickGuess.Models;
using QuickGuess.Services.Game;
using System.Security.Claims;

namespace QuickGuess.Controllers
{
    [ApiController]
    [Route("api/guess")]
    [Authorize]
    public class GuessController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public GuessController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost("song")]
        public async Task<IActionResult> GuessSong(GuessRequest request)
            => await ProcessGuess(request, "song");

        [HttpPost("movie")]
        public async Task<IActionResult> GuessMovie(GuessRequest request)
            => await ProcessGuess(request, "movie");

        private async Task<IActionResult> ProcessGuess(GuessRequest request, string type)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            string correctTitle;
            if (type == "song")
            {
                var song = await _db.Songs.FindAsync(request.ItemId);
                if (song == null) return NotFound("Song not found");
                correctTitle = song.Title;
            }
            else
            {
                var movie = await _db.Movies.FindAsync(request.ItemId);
                if (movie == null) return NotFound("Movie not found");
                correctTitle = movie.Title;
            }

            bool correct = string.Equals(request.GuessText.Trim(), correctTitle.Trim(), StringComparison.OrdinalIgnoreCase);
            int score = ScoreCalculator.CalculateScore(correct, request.Duration);

            var guess = new Guess
            {
                UserId = userId,
                Type = type,
                ItemId = request.ItemId,
                Correct = correct,
                GuessText = request.GuessText,
                Duration = request.Duration,
                Score = score,
                Mode = request.Mode
            };

            _db.Guesses.Add(guess);

            if (request.Mode == "ranking")
            {
                var board = await _db.Leaderboards.FindAsync(userId);
                if (board == null)
                {
                    board = new Leaderboard { UserId = userId };
                    _db.Leaderboards.Add(board);
                }

                board.ScoreTotal += score;
                if (type == "song") board.ScoreSongs += score;
                else board.ScoreMovies += score;
            }

            await _db.SaveChangesAsync();

            int totalScore = 0;
            int position = 0;

            if (request.Mode == "ranking")
            {
                var board = await _db.Leaderboards.FindAsync(userId);
                totalScore = board?.ScoreTotal ?? 0;

                // oblicz ranking (liczba osób z większym wynikiem + 1)
                position = await _db.Leaderboards.CountAsync(b => b.ScoreTotal > totalScore) + 1;
            }

            return Ok(new
            {
                Correct = correct,
                Score = score,
                TotalScore = totalScore,
                RankingPosition = position,
                CorrectTitle = correctTitle
            });

        }
    }
}

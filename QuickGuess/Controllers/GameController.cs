using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickGuess.Data;
using QuickGuess.Models;
using System.Security.Claims;

namespace QuickGuess.Controllers
{
    [ApiController]
    [Route("api/game")]
    [Authorize]
    public class GameController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public GameController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartGame([FromBody] StartGameRequest req)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var session = new GameSession
            {
                Id = req.SessionId,
                UserId = userId,
                Mode = req.Mode ?? "ranking",
                Type = req.Type ?? "song",
                StartTime = DateTime.UtcNow,
                Finished = false
            };

            _db.GameSessions.Add(session);
            await _db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("abandon")]
        public async Task<IActionResult> Abandon([FromBody] AbandonRequest req)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var session = await _db.GameSessions.FirstOrDefaultAsync(s =>
                s.Id == req.SessionId && s.UserId == userId && !s.Finished);

            if (session == null)
                return NotFound();

            session.Finished = true;
            await PenalizeUser(userId, session.Type); 
            await _db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("finish")]
        public async Task<IActionResult> FinishGame([FromBody] AbandonRequest req)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var session = await _db.GameSessions.FirstOrDefaultAsync(s =>
                s.Id == req.SessionId && s.UserId == userId && !s.Finished);

            if (session == null)
                return NotFound();

            session.Finished = true; 
            await _db.SaveChangesAsync();

            return Ok();
        }


        private async Task PenalizeUser(Guid userId, string type)
        {
            var penalty = -50; 
            var board = await _db.Leaderboards.FindAsync(userId);
            if (board == null)
            {
                board = new Leaderboard { UserId = userId };
                _db.Leaderboards.Add(board);
            }

            board.ScoreTotal = Math.Max(0, board.ScoreTotal + penalty);

            if (type == "song")
                board.ScoreSongs = Math.Max(0, board.ScoreSongs + penalty);
            else
                board.ScoreMovies = Math.Max(0, board.ScoreMovies + penalty);
        }
    }

    public record StartGameRequest(Guid SessionId, string Mode, string Type);
    public record AbandonRequest(Guid SessionId);
}

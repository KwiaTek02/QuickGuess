using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickGuess.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace QuickGuess.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ProfileController(ApplicationDbContext db) => _db = db;

        public class ProfileDto
        {
            public Guid PublicId { get; set; }
            public string Username { get; set; } = "";
            public string? Note { get; set; }
            public DateTime CreatedAt { get; set; }
            public int TotalScoreSongs { get; set; }
            public int TotalScoreMovies { get; set; }
        }

        public class ProfileLookupDto
        {
            public Guid PublicId { get; set; }
            public string Username { get; set; } = "";
        }

        [HttpGet("{publicId:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProfileDto>> GetByPublicId(Guid publicId)
        {
            var user = await _db.Users
                .Where(u => u.PublicId.HasValue && u.PublicId.Value == publicId)
                .Select(u => new ProfileDto
                {
                    PublicId = u.PublicId!.Value,
                    Username = u.Username,
                    Note = u.ProfileNote,
                    CreatedAt = u.CreatedAt,
                    TotalScoreSongs = _db.Leaderboards
                        .Where(l => l.UserId == u.Id)
                        .Select(l => (int?)l.ScoreSongs).FirstOrDefault() ?? 0,
                    TotalScoreMovies = _db.Leaderboards
                        .Where(l => l.UserId == u.Id)
                        .Select(l => (int?)l.ScoreMovies).FirstOrDefault() ?? 0
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();
            return user;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ProfileDto>> Me()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (string.IsNullOrWhiteSpace(email)) return Unauthorized();

            var user = await _db.Users.Where(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null) return NotFound();

            if (user.PublicId == Guid.Empty)
            {
                user.PublicId = Guid.NewGuid();
                await _db.SaveChangesAsync();
            }

            var dto = new ProfileDto
            {
                PublicId = user.PublicId!.Value,
                Username = user.Username,
                Note = user.ProfileNote,
                CreatedAt = user.CreatedAt,
                TotalScoreSongs = await _db.Leaderboards.Where(l => l.UserId == user.Id)
                    .Select(l => (int?)l.ScoreSongs).FirstOrDefaultAsync() ?? 0,
                TotalScoreMovies = await _db.Leaderboards.Where(l => l.UserId == user.Id)
                    .Select(l => (int?)l.ScoreMovies).FirstOrDefaultAsync() ?? 0
            };
            return dto;
        }

        public class UpdateNoteDto { public string? Note { get; set; } }

        [HttpPut("me/note")]
        [Authorize]
        public async Task<IActionResult> UpdateNote(UpdateNoteDto dto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (string.IsNullOrWhiteSpace(email)) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound();

            user.ProfileNote = (dto.Note ?? "").Trim();
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("lookup")]
        [AllowAnonymous]
        public async Task<ActionResult<ProfileLookupDto>> Lookup([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest();

            var user = await _db.Users
                .Where(u => u.Username.ToLower() == username.ToLower())
                .FirstOrDefaultAsync();

            if (user is null)
                return NotFound();

            if (user.PublicId == null || user.PublicId == Guid.Empty)
            {
                user.PublicId = Guid.NewGuid();
                await _db.SaveChangesAsync();
            }

            return new ProfileLookupDto
            {
                PublicId = user.PublicId!.Value,
                Username = user.Username
            };
        }

        [HttpGet("username/{username}")]
        [AllowAnonymous]
        public Task<ActionResult<ProfileLookupDto>> LookupByRoute(string username)
            => Lookup(username);

    }
}

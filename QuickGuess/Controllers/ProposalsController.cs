using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickGuess.Data;
using QuickGuess.Models;
using System;
using System.Linq;
using System.Security.Claims;

namespace QuickGuess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class ProposalsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ProposalsController(ApplicationDbContext db) { _db = db; }

        public class CreateProposalDto
        {
            public string Type { get; set; } = "";  
            public string Title { get; set; } = "";
            public string? ArtistOrNote { get; set; }
        }
        private static bool IsAdminUser(ClaimsPrincipal user)
        {
            if (user.IsInRole("Admin")) return true;
            if (user.HasClaim("is_admin", "true")) return true;

            var roleValues =
                user.FindAll(ClaimTypes.Role).Select(c => c.Value)
                    .Concat(user.FindAll("role").Select(c => c.Value))
                    .Concat(user.FindAll("roles").Select(c => c.Value));

            foreach (var v in roleValues)
            {
                var tokens = v.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Any(t => string.Equals(t, "Admin", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            return false;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProposalDto dto)
        {
            var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(uidStr, out var userId)) return Unauthorized("Brak identyfikatora użytkownika.");

            var type = (dto.Type ?? "").Trim().ToLowerInvariant();
            if (type != "song" && type != "movie") return BadRequest("Typ musi być 'song' lub 'movie'.");

            var title = (dto.Title ?? "").Trim();
            if (title.Length < 2 || title.Length > 120) 
                return BadRequest("Tytuł musi mieć 2–120 znaków.");

            var note = string.IsNullOrWhiteSpace(dto.ArtistOrNote) ? null : dto.ArtistOrNote!.Trim(); 
            if (note is { Length: > 50 }) 
                return BadRequest("Notatka może mieć maks. 50 znaków.");

            var isAdmin = IsAdminUser(User); 

            if (!isAdmin) 
            {
                var today = DateTime.UtcNow.Date;
                var used = await _db.ProposedTitles
                    .CountAsync(p => p.UserId == userId
                                  && p.Type == type
                                  && p.CreatedAt >= today
                                  && p.CreatedAt < today.AddDays(1));

                if (used >= 5)
                    return BadRequest("Wykorzystano dzienny limit 5 propozycji dla tego typu.");
            }

            var proposal = new ProposedTitle
            {
                UserId = userId,
                Type = type,
                Title = title,
                ArtistOrNote = note, 
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.ProposedTitles.Add(proposal);
            await _db.SaveChangesAsync();
            return Ok(proposal);
        }

        [HttpGet("my-counts")]
        public async Task<IActionResult> MyCounts()
        {
            var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(uidStr, out var userId)) return Unauthorized();

            var now = DateTime.UtcNow.Date;
            var songs = await _db.ProposedTitles.CountAsync(p => p.UserId == userId && p.Type == "song" && p.CreatedAt >= now && p.CreatedAt < now.AddDays(1));
            var movies = await _db.ProposedTitles.CountAsync(p => p.UserId == userId && p.Type == "movie" && p.CreatedAt >= now && p.CreatedAt < now.AddDays(1));

            return Ok(new { songs, movies });
        }
    }
}

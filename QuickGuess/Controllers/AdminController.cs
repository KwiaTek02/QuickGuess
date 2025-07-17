using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickGuess.Data;

namespace QuickGuess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("pending-proposals")]
        public async Task<IActionResult> GetPendingProposals()
        {
            var pending = await _db.ProposedTitles
                .Include(p => p.User)
                .Where(p => p.Status == "pending")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(pending);
        }

        [HttpPost("review/{id:guid}")]
        public async Task<IActionResult> ReviewProposal(Guid id, [FromQuery] string action, [FromBody] string? note = null)
        {
            var proposal = await _db.ProposedTitles.FindAsync(id);
            if (proposal == null) return NotFound();

            if (action != "approve" && action != "reject")
                return BadRequest("Action must be 'approve' or 'reject'");

            proposal.Status = action == "approve" ? "approved" : "rejected";
            proposal.AdminNote = note;
            proposal.ReviewedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(proposal);
        }
    }
}

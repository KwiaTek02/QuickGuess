using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
    [Table("proposed_titles")]
    public class ProposedTitle
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Type { get; set; } = null!; // 'song' lub 'movie'
        public string Title { get; set; } = null!;
        public string? ArtistOrNote { get; set; }
        public string Status { get; set; } = "pending"; // 'pending', 'approved', 'rejected'
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        public User? User { get; set; } // relacja
    }
}

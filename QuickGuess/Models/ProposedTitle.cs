using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
    [Table("proposed_titles")]
    public class ProposedTitle
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("type")]
        public string Type { get; set; } = null!;

        [Required]
        [Column("title")]
        public string Title { get; set; } = null!;

        [Column("artist_or_note")]
        public string? ArtistOrNote { get; set; }

        [Required]
        [Column("status")]
        public string Status { get; set; } = "pending";

        [Column("admin_note")]
        public string? AdminNote { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("reviewed_at")]
        public DateTime? ReviewedAt { get; set; }

        public User? User { get; set; }
    }
}
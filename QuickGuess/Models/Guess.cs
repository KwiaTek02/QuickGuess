using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
    [Table("guesses")]
    public class Guess
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
        [Column("item_id")]
        public Guid ItemId { get; set; }

        [Required]
        [Column("correct")]
        public bool Correct { get; set; }

        [Required]
        [Column("guess_text")]
        public string GuessText { get; set; } = null!;

        [Required]
        [Column("duration")]
        public int Duration { get; set; }

        [Required]
        [Column("score")]
        public int Score { get; set; }

        [Required]
        [Column("mode")]
        public string Mode { get; set; } = null!;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
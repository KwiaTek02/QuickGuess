using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
        [Table("guesses")]
    public class Guess
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Type { get; set; } = null!; // song or movie
        public Guid ItemId { get; set; }
        public bool Correct { get; set; }
        public string GuessText { get; set; } = null!;
        public int Duration { get; set; }
        public int Score { get; set; }
        public string Mode { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

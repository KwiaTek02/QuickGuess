using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
    [Table("songs")]
    public class Song
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = null!;
        public string Artist { get; set; } = null!;
        public int? ReleaseYear { get; set; }
        public string AudioUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

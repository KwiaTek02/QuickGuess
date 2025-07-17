using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
    [Table("movies")]
    public class Movie
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = null!;
        public string? Director { get; set; }
        public int? ReleaseYear { get; set; }
        public string ImageUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

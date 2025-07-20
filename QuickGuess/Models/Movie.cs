using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
    [Table("movies")]
    public class Movie
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("title")]
        public string Title { get; set; } = null!;

        [Column("director")]
        public string? Director { get; set; }

        [Column("release_year")]
        public int? ReleaseYear { get; set; }

        [Required]
        [Column("image_url")]
        public string ImageUrl { get; set; } = null!;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
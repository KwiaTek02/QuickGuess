using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
    [Table("songs")]
    public class Song
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("title")]
        public string Title { get; set; } = null!;

        [Required]
        [Column("artist")]
        public string Artist { get; set; } = null!;


        [Column("release_year")]
        public int? ReleaseYear { get; set; }

        [Required]
        [Column("audio_url")]
        public string AudioUrl { get; set; } = null!;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

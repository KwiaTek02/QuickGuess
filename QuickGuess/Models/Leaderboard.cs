using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
    [Table("leaderboards")]
    public class Leaderboard
    {
        [Key]
        [ForeignKey("User")]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("score_total")]
        public int ScoreTotal { get; set; }

        [Column("score_songs")]
        public int ScoreSongs { get; set; }

        [Column("score_movies")]
        public int ScoreMovies { get; set; }

        public User? User { get; set; }
    }
}
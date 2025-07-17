using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
    [Table("leaderboards")]
    public class Leaderboard
    {
        [Key]
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public int ScoreTotal { get; set; }
        public int ScoreSongs { get; set; }
        public int ScoreMovies { get; set; }

        public User? User { get; set; }
    }
}

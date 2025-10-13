using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickGuess.Models
{
    [Table("game_sessions")]
    public class GameSession
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("mode")]
        public string Mode { get; set; } = "ranking";

        [Column("type")]
        public string Type { get; set; } = "song"; 

        [Column("start_time")]
        public DateTime StartTime { get; set; }

        [Column("finished")]
        public bool Finished { get; set; } = false;
    }
}
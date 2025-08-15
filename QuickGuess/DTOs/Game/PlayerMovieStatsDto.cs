namespace QuickGuess.DTOs.Game
{
    public class PlayerMovieStatsDto
    {
        public int Games { get; set; }
        public int Correct { get; set; }
        public int Incorrect { get; set; }
        public double WinRate { get; set; }
        public int RankingPosition { get; set; }
        public int BestTimeSec { get; set; }
        public double AvgTimeSec { get; set; }
        public int TotalScore { get; set; }
    }
}

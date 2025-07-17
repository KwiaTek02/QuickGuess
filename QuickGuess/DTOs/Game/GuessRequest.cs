namespace QuickGuess.DTOs.Game
{
    public class GuessRequest
    {
        public Guid ItemId { get; set; }
        public string GuessText { get; set; } = string.Empty;
        public string Mode { get; set; } = "training"; // training or ranking
        public int Duration { get; set; } // in seconds
    }
}

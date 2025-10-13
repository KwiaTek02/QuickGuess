namespace QuickGuess.DTOs.Game
{
    public class GuessRequest
    {
        public Guid ItemId { get; set; }
        public string GuessText { get; set; } = string.Empty;
        public string Mode { get; set; } = "training"; 
        public int Duration { get; set; } 
    }
}

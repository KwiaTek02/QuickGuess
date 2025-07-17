namespace QuickGuess.Services.Game
{
    public static class ScoreCalculator
    {
        public static int CalculateScore(bool correct, int duration)
        {
            if (!correct) return -5;
            int baseScore = 100;
            int timePenalty = duration * 4;
            return Math.Max(baseScore - timePenalty, 1); // zawsze min. 1 pkt
        }
    }
}

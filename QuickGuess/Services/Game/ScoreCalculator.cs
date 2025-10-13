namespace QuickGuess.Services.Game
{
    public static class ScoreCalculator
    {
        public static int CalculateScore(bool correct, int durationMs)
        {
            if (!correct) return -50; 

            const int baseScore = 100;

            int timePenalty = (durationMs + 199) / 200;

            
            timePenalty = Math.Min(timePenalty, baseScore);

            return Math.Max(baseScore - timePenalty, 1);
        }
    }
}
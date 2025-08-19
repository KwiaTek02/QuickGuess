namespace QuickGuess.Services.Game
{
    public static class ScoreCalculator
    {
        /// <summary>
        /// durationMs – czas odpowiedzi w milisekundach.
        /// Kara: 1 punkt za KAŻDE rozpoczęte 200 ms (0,2 s) = 5 pkt/sek.
        /// Minimalny wynik rundy: 1 punkt (gdy odpowiedź poprawna).
        /// </summary>
        public static int CalculateScore(bool correct, int durationMs)
        {
            if (!correct) return -50; 

            const int baseScore = 100;

            // ceil(durationMs / 200) bez double:
            int timePenalty = (durationMs + 199) / 200;

            // (opcjonalnie) nie pozwól zabrać więcej niż 100:
            timePenalty = Math.Min(timePenalty, baseScore);

            return Math.Max(baseScore - timePenalty, 1);
        }
    }
}
using Microsoft.EntityFrameworkCore;
using QuickGuess.Data;

namespace QuickGuess.Services.Game
{
    public class GameSessionCleaner : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public GameSessionCleaner(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var now = DateTime.UtcNow;
                var expired = await db.GameSessions
                    .Where(s => !s.Finished && now - s.StartTime > TimeSpan.FromSeconds(20))
                    .ToListAsync(stoppingToken);

                foreach (var session in expired)
                {
                    
                    session.Finished = true;
                }

                await db.SaveChangesAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
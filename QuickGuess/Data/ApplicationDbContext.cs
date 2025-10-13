using QuickGuess.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QuickGuess.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<ProposedTitle> ProposedTitles => Set<ProposedTitle>();
        public DbSet<Guess> Guesses => Set<Guess>();
        public DbSet<Leaderboard> Leaderboards => Set<Leaderboard>();
        public DbSet<Song> Songs => Set<Song>();
        public DbSet<Movie> Movies => Set<Movie>();
        public DbSet<GameSession> GameSessions { get; set; }

    }
}

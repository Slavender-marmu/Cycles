using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BicycleRacing.Database.Entities;

namespace BicycleRacing.Database
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User>(options)
    {
        public DbSet<Race> Races => Set<Race>();

        public DbSet<Score> Scores => Set<Score>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Race>()
                .HasMany(race => race.Participants)
                .WithMany(user => user.Races)
                .UsingEntity<Dictionary<string, object>>(
                    "RaceParticipants",
                    participants => participants.HasOne<User>().WithMany().HasForeignKey("UserId"),
                    participants => participants.HasOne<Race>().WithMany().HasForeignKey("RaceId"),
                    participants => participants.HasKey("RaceId", "UserId"));

            // Store elapsed ticks rather than SQL Server time, which is limited to 24 hours.
            builder.Entity<Score>()
                .Property(score => score.Time)
                .HasConversion(time => time.Ticks, ticks => TimeSpan.FromTicks(ticks));
        }
    }
}

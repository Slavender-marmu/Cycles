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

            builder.Entity<Race>()
                .HasMany(race => race.Organizers)
                .WithMany(user => user.OrganizedRaces)
                .UsingEntity<Dictionary<string, object>>(
                    "RaceOrganizers",
                    organizers => organizers.HasOne<User>().WithMany().HasForeignKey("UserId"),
                    organizers => organizers.HasOne<Race>().WithMany().HasForeignKey("RaceId"),
                    organizers => organizers.HasKey("RaceId", "UserId"));

            builder.Entity<User>()
                .HasMany(user => user.Friends)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "UserFriends",
                    friends => friends.HasOne<User>().WithMany().HasForeignKey("FriendId")
                        .OnDelete(DeleteBehavior.Restrict),
                    friends => friends.HasOne<User>().WithMany().HasForeignKey("UserId"),
                    friends =>
                    {
                        friends.HasKey("UserId", "FriendId");
                        friends.ToTable(table => table.HasCheckConstraint(
                            "CK_UserFriends_DifferentUsers", "[UserId] <> [FriendId]"));
                    });

            // Store elapsed ticks rather than SQL Server time, which is limited to 24 hours.
            builder.Entity<Score>()
                .Property(score => score.Time)
                .HasConversion(time => time.Ticks, ticks => TimeSpan.FromTicks(ticks));
        }
    }
}

using BicycleRacing.Database;
using BicycleRacing.Database.Entities;
using BicycleRacing.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BicycleRacing.Tests;

public class UserRelationshipsTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private ApplicationDbContext database = null!;
    private FriendshipService friendships = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        database = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection).Options);
        await database.Database.EnsureCreatedAsync();
        database.Users.AddRange(new User { Id = "alice" }, new User { Id = "bob" });
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        friendships = new FriendshipService(database);
    }

    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task AddingFriendPersistsBothDirections()
    {
        await friendships.AddFriendAsync("alice", "bob");
        database.ChangeTracker.Clear();

        var alice = await database.Users.Include(user => user.Friends).SingleAsync(user => user.Id == "alice");
        var bob = await database.Users.Include(user => user.Friends).SingleAsync(user => user.Id == "bob");

        Assert.Equal("bob", Assert.Single(alice.Friends).Id);
        Assert.Equal("alice", Assert.Single(bob.Friends).Id);
    }

    [Fact]
    public async Task AddingExistingFriendFromEitherSideDoesNotDuplicateLinks()
    {
        await friendships.AddFriendAsync("alice", "bob");
        database.ChangeTracker.Clear();
        await friendships.AddFriendAsync("bob", "alice");
        await friendships.AddFriendAsync("alice", "bob");
        database.ChangeTracker.Clear();

        var users = await database.Users.Include(user => user.Friends).ToListAsync();
        Assert.All(users, user => Assert.Single(user.Friends));
    }

    [Fact]
    public async Task RemovingFriendFromEitherSideRemovesBothDirectionsAndCanBeRepeated()
    {
        await friendships.AddFriendAsync("alice", "bob");
        database.ChangeTracker.Clear();
        await friendships.RemoveFriendAsync("bob", "alice");
        database.ChangeTracker.Clear();
        await friendships.RemoveFriendAsync("alice", "bob");
        database.ChangeTracker.Clear();

        var users = await database.Users.Include(user => user.Friends).ToListAsync();
        Assert.All(users, user => Assert.Empty(user.Friends));
    }

    [Fact]
    public async Task SelfFriendshipsAreRejectedByServiceAndDatabase()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => friendships.AddFriendAsync("alice", "alice"));
        await Assert.ThrowsAsync<SqliteException>(() => database.Database.ExecuteSqlRawAsync(
            "INSERT INTO UserFriends (UserId, FriendId) VALUES ('alice', 'alice')"));
    }

    [Fact]
    public async Task MissingFriendDoesNotCreateAnyLinks()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => friendships.AddFriendAsync("alice", "missing"));
        database.ChangeTracker.Clear();

        var alice = await database.Users.Include(user => user.Friends).SingleAsync(user => user.Id == "alice");
        Assert.Empty(alice.Friends);
    }

    [Fact]
    public async Task RaceCanHaveMultipleOrganizersWithoutRegisteringThemAsParticipants()
    {
        var users = await database.Users.ToListAsync();
        var race = new Race { Name = "Road race", Organizers = users };
        var secondRace = new Race { Name = "Second race", Organizers = [users[0]] };
        database.Races.AddRange(race, secondRace);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var savedRace = await database.Races.Include(race => race.Organizers)
            .Include(race => race.Participants).SingleAsync(saved => saved.Id == race.Id);
        var organizer = await database.Users.Include(user => user.OrganizedRaces)
            .SingleAsync(user => user.Id == users[0].Id);

        Assert.Equal(2, savedRace.Organizers.Count);
        Assert.Empty(savedRace.Participants);
        Assert.Equal(2, organizer.OrganizedRaces.Count);

        database.Races.Remove(savedRace);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        Assert.Equal(2, await database.Users.CountAsync());
        organizer = await database.Users.Include(user => user.OrganizedRaces)
            .SingleAsync(user => user.Id == users[0].Id);
        Assert.Equal(secondRace.Id, Assert.Single(organizer.OrganizedRaces).Id);
    }
}

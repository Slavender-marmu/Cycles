using BicycleRacing.Database;
using BicycleRacing.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace BicycleRacing.Services;

public class FriendshipService(ApplicationDbContext database)
{
    public async Task AddFriendAsync(string userId, string friendId, CancellationToken cancellationToken = default)
    {
        var (user, friend) = await GetUsersAsync(userId, friendId, cancellationToken);

        if (!user.Friends.Contains(friend))
        {
            user.Friends.Add(friend);
        }

        if (!friend.Friends.Contains(user))
        {
            friend.Friends.Add(user);
        }

        // Both directions are persisted together by EF Core's SaveChanges transaction.
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFriendAsync(string userId, string friendId, CancellationToken cancellationToken = default)
    {
        var (user, friend) = await GetUsersAsync(userId, friendId, cancellationToken);

        user.Friends.Remove(friend);
        friend.Friends.Remove(user);

        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task<(User User, User Friend)> GetUsersAsync(
        string userId, string friendId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(friendId);

        if (userId == friendId)
        {
            throw new ArgumentException("A user cannot be their own friend.", nameof(friendId));
        }

        var users = await database.Users
            .Where(user => user.Id == userId || user.Id == friendId)
            .Include(user => user.Friends)
            .ToListAsync(cancellationToken);

        var user = users.SingleOrDefault(user => user.Id == userId)
            ?? throw new KeyNotFoundException("The user does not exist.");
        var friend = users.SingleOrDefault(user => user.Id == friendId)
            ?? throw new KeyNotFoundException("The friend does not exist.");

        return (user, friend);
    }
}

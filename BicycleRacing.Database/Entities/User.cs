using Microsoft.AspNetCore.Identity;

namespace BicycleRacing.Database.Entities;

public class User : IdentityUser
{
    public ICollection<Race> Races { get; set; } = [];

    public ICollection<Race> OrganizedRaces { get; set; } = [];

    public ICollection<User> Friends { get; set; } = [];

    public ICollection<Score> Scores { get; set; } = [];
}

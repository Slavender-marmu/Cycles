using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BicycleRacing.Database.Entities;

[Index(nameof(RaceId), nameof(UserId), IsUnique = true)]
public class Score
{
    public int Id { get; set; }

    public int RaceId { get; set; }

    public Race Race { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User User { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int Placement { get; set; }

    [Range(typeof(TimeSpan), "00:00:00", "10675199.02:48:05.4775807",
        ErrorMessage = "Finishing time cannot be negative.")]
    public TimeSpan Time { get; set; }
}

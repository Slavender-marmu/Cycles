using System.ComponentModel.DataAnnotations;

namespace BicycleRacing.Database.Entities;

public class Race
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [DataType(DataType.DateTime)]
    public DateTimeOffset StartDate { get; set; }

    [DataType(DataType.DateTime)]
    public DateTimeOffset? EndDate { get; set; }

    public ICollection<User> Participants { get; set; } = [];

    public ICollection<Score> Scores { get; set; } = [];
}

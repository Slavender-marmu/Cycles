using System.ComponentModel.DataAnnotations;
using BicycleRacing.Database.Enums;

namespace BicycleRacing.Database.Entities;

public class Race
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [EnumDataType(typeof(RaceCategory))]
    public RaceCategory Category { get; set; } = RaceCategory.Unspecified;

    [EnumDataType(typeof(RaceStatus))]
    public RaceStatus Status { get; set; } = RaceStatus.Draft;

    [DataType(DataType.DateTime)]
    public DateTimeOffset StartDate { get; set; }

    [DataType(DataType.DateTime)]
    public DateTimeOffset? EndDate { get; set; }

    public ICollection<User> Participants { get; set; } = [];

    public ICollection<User> Organizers { get; set; } = [];

    public ICollection<Score> Scores { get; set; } = [];
}

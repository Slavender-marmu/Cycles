using System.ComponentModel.DataAnnotations;

namespace BicycleRacing.Database.Enums;

public enum RaceCategory
{
    Unspecified = 0,
    Road = 1,

    [Display(Name = "Mountain Bike")]
    MountainBike = 2,

    Gravel = 3,
    Cyclocross = 4,
    Track = 5,

    [Display(Name = "BMX")]
    Bmx = 6
}

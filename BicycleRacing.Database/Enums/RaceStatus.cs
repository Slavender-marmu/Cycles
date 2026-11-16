using System.ComponentModel.DataAnnotations;

namespace BicycleRacing.Database.Enums;

public enum RaceStatus
{
    Draft = 0,
    Scheduled = 1,

    [Display(Name = "Registration Open")]
    RegistrationOpen = 2,

    [Display(Name = "Registration Closed")]
    RegistrationClosed = 3,

    [Display(Name = "In Progress")]
    InProgress = 4,

    Completed = 5,
    Cancelled = 6
}

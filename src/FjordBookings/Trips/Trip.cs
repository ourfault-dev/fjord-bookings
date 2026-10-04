namespace FjordBookings.Trips;

/// <summary>A boat trip on the timetable, priced per passenger in Norwegian kroner before the season's multiplier.</summary>
public sealed record Trip(
    string Code,
    string Name,
    string Fjord,
    string Summary,
    string Quay,
    TimeOnly Departs,
    TimeSpan Duration,
    decimal BasePrice);

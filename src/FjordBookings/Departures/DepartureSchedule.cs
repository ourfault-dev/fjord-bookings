using FjordBookings.Trips;

namespace FjordBookings.Departures;

/// <summary>One sailing of a trip, at its local departure time.</summary>
public sealed record Departure(string TripCode, DateTime Departs, string Quay);

/// <summary>The timetable: every trip sails once a day at its departure time, a week either side of the date asked about.</summary>
public sealed class DepartureSchedule
{
    private const int DaysEitherSide = 7;

    public IEnumerable<Departure> Around(Trip trip, DateOnly date)
    {
        for (var offset = -DaysEitherSide; offset <= DaysEitherSide; offset++)
        {
            yield return new Departure(trip.Code, date.AddDays(offset).ToDateTime(trip.Departs), trip.Quay);
        }
    }

    /// <summary>The sailing of the trip on the date given.</summary>
    public Departure DepartureOn(Trip trip, DateOnly date)
    {
        var day = date.ToDateTime(TimeOnly.MinValue);
        return Around(trip, date).First(departure => departure.Departs == day);
    }
}

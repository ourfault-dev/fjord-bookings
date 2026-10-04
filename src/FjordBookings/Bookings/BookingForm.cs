using System.Globalization;
using FjordBookings.Trips;
using Microsoft.Extensions.Primitives;

namespace FjordBookings.Bookings;

/// <summary>The booking form's fields as submitted, the request they make when they are valid, and what is wrong when not.</summary>
public sealed class BookingForm
{
    public const int MaxPassengers = 12;

    public string Trip { get; init; } = "";
    public string Date { get; init; } = "";
    public string Passengers { get; init; } = "2";
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";

    public Dictionary<string, string> Errors { get; } = [];

    public static BookingForm From(IFormCollection form) => new()
    {
        Trip = Field(form["trip"]),
        Date = Field(form["date"]),
        Passengers = Field(form["passengers"]),
        Name = Field(form["name"]),
        Email = Field(form["email"]),
    };

    private static string Field(StringValues value) => value.ToString().Trim();

    /// <summary>The request the form makes, or null with <see cref="Errors"/> filled in.</summary>
    public BookingRequest? Validate(TripCatalogue trips, DateOnly today)
    {
        Errors.Clear();
        var trip = trips.Find(Trip);
        if (trip is null)
        {
            Errors["trip"] = "Choose one of the trips.";
        }
        if (!DateOnly.TryParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            Errors["date"] = "Pick the day you would like to sail.";
        }
        else if (date < today)
        {
            Errors["date"] = "That day has already sailed. Pick a day from today on.";
        }
        if (!int.TryParse(Passengers, NumberStyles.None, CultureInfo.InvariantCulture, out var passengers) || passengers is < 1 or > MaxPassengers)
        {
            Errors["passengers"] = $"Between 1 and {MaxPassengers} passengers can book online.";
        }
        if (Name.Length < 2)
        {
            Errors["name"] = "Tell us whose name the booking is under.";
        }
        if (!Email.Contains('@', StringComparison.Ordinal) || Email.Length < 5)
        {
            Errors["email"] = "We send the tickets to this address.";
        }
        return Errors.Count == 0 ? new BookingRequest(trip!.Code, date, passengers, Name, Email) : null;
    }
}

using FjordBookings.Trips;

namespace FjordBookings.Pricing;

/// <summary>A price for a party on one sailing: the trip's base fare per passenger, times the season's multiplier.</summary>
public sealed record Quote(decimal PerPassenger, int Passengers, decimal Total);

public sealed class PricingService(SeasonalRates rates)
{
    /// <summary>The price for <paramref name="passengers"/> on the trip's sailing on <paramref name="date"/>.</summary>
    public Quote Quote(Trip trip, DateOnly date, int passengers)
    {
        var multiplier = rates.MultiplierFor(date);
        var perPassenger = decimal.Round(trip.BasePrice * multiplier, 0, MidpointRounding.AwayFromZero);
        return new Quote(perPassenger, passengers, perPassenger * passengers);
    }
}

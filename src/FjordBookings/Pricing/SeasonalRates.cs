using System.Globalization;

namespace FjordBookings.Pricing;

/// <summary>
/// The season's price multipliers, one per month: the shoulder months sit near the base fare, high summer above it.
/// </summary>
public sealed class SeasonalRates
{
    private static readonly Dictionary<string, decimal> Multipliers = new()
    {
        ["jan"] = 0.80m,
        ["feb"] = 0.80m,
        ["mar"] = 0.85m,
        ["apr"] = 0.90m,
        ["may"] = 1.00m,
        ["jun"] = 1.20m,
        ["jul"] = 1.35m,
        ["aug"] = 1.30m,
        ["sep"] = 1.05m,
        ["oct"] = 0.95m,
        ["nov"] = 0.85m,
        ["dec"] = 0.90m,
    };

    /// <summary>The multiplier for the month the trip sails in.</summary>
    public decimal MultiplierFor(DateOnly date)
    {
        var month = date.ToString("MMM", CultureInfo.InvariantCulture);
        return Multipliers[month];
    }
}

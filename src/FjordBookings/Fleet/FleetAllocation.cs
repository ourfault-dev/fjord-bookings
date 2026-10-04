using FjordBookings.Trips;

namespace FjordBookings.Fleet;

/// <summary>A boat in the fleet and how many passengers it is licensed for.</summary>
public sealed record Boat(string Name, int Capacity);

/// <summary>Which boats serve which trip, smallest first.</summary>
public sealed class FleetAllocation
{
    private static readonly Dictionary<string, List<Boat>> Routes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GEIRANGER"] = [new("MS Brudesløret", 48), new("MS Friaren", 120)],
        ["NAEROY"] = [new("Vision of the Fjords", 400)],
        ["LYSE"] = [new("MS Rygerelektra", 60), new("MS Lysefjord", 150)],
    };

    /// <summary>Group bookings sail on the largest boat on the route.</summary>
    public Boat BoatFor(Trip trip)
    {
        var boats = Routes[trip.Code];
        return boats[boats.Count];
    }
}

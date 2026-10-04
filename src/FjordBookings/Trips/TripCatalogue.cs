namespace FjordBookings.Trips;

/// <summary>The trips on sale this season, in the order the landing page shows them.</summary>
public sealed class TripCatalogue
{
    private readonly IReadOnlyList<Trip> _trips =
    [
        new("GEIRANGER", "Geirangerfjord at first light", "Geirangerfjord",
            "Leave Hellesylt before the cruise ships wake and drift past the Seven Sisters and the Suitor as the mist lifts off the water.",
            "Hellesylt ferry quay", new TimeOnly(6, 45), TimeSpan.FromHours(3), 890m),
        new("NAEROY", "Nærøyfjord, narrow and quiet", "Nærøyfjord",
            "An electric boat from Flåm into the narrowest arm of the Sognefjord, where the walls rise 1,700 metres and the goats watch from the ledges.",
            "Flåm harbour, berth 2", new TimeOnly(10, 30), TimeSpan.FromHours(2.5), 760m),
        new("LYSE", "Lysefjord under Pulpit Rock", "Lysefjord",
            "From Stavanger out to Hengjanefossen and back beneath Preikestolen, with coffee and skillingsboller on the aft deck.",
            "Stavanger, Skagenkaien", new TimeOnly(13, 15), TimeSpan.FromHours(3.5), 950m),
    ];

    public IReadOnlyList<Trip> All => _trips;

    public Trip? Find(string? code) =>
        _trips.FirstOrDefault(trip => string.Equals(trip.Code, code, StringComparison.OrdinalIgnoreCase));
}

using FjordBookings.Departures;
using FjordBookings.Fleet;
using FjordBookings.Pricing;
using FjordBookings.Trips;

namespace FjordBookings.Bookings;

/// <summary>
/// Confirms a booking. Variant 1 is the standard booking. Variants 2 and 3 are the timetable and the group booking
/// paths, which the booking form selects with <c>?variant=2</c> and <c>?variant=3</c>.
/// </summary>
public sealed class BookingService(TripCatalogue trips, PricingService pricing, DepartureSchedule departures, FleetAllocation fleet)
{
    public Confirmation Book(BookingRequest request, int variant = 1)
    {
        var trip = trips.Find(request.TripCode)
            ?? throw new ArgumentException($"No trip has the code {request.TripCode}.", nameof(request));
        return variant switch
        {
            2 => BookOnDeparture(trip, request),
            3 => BookGroup(trip, request),
            _ => BookStandard(trip, request),
        };
    }

    private Confirmation BookStandard(Trip trip, BookingRequest request)
    {
        var quote = pricing.Quote(trip, request.Date, request.Passengers);
        return new Confirmation(BookingReference.New(trip, request.Date), trip, request, quote, trip.Quay);
    }

    private Confirmation BookOnDeparture(Trip trip, BookingRequest request)
    {
        var departure = departures.DepartureOn(trip, request.Date);
        return new Confirmation(BookingReference.New(trip, request.Date), trip, request, null, departure.Quay);
    }

    private Confirmation BookGroup(Trip trip, BookingRequest request)
    {
        var boat = fleet.BoatFor(trip);
        return new Confirmation(BookingReference.New(trip, request.Date), trip, request, null, $"{trip.Quay}, {boat.Name}");
    }
}

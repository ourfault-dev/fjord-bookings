using System.Security.Cryptography;
using FjordBookings.Trips;

namespace FjordBookings.Bookings;

/// <summary>Booking references such as <c>GEI-0714-K7Q2</c>: the trip, the sailing's month and day, and four random characters.</summary>
public static class BookingReference
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string New(Trip trip, DateOnly date) =>
        $"{trip.Code[..3]}-{date:MMdd}-{RandomNumberGenerator.GetString(Alphabet, 4)}";
}

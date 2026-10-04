namespace FjordBookings.Bookings;

/// <summary>What the booking form asks for, once it has been checked.</summary>
public sealed record BookingRequest(string TripCode, DateOnly Date, int Passengers, string Name, string Email);

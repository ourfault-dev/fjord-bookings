using FjordBookings.Pricing;
using FjordBookings.Trips;

namespace FjordBookings.Bookings;

/// <summary>A confirmed booking, as the confirmation page shows it.</summary>
public sealed record Confirmation(string Reference, Trip Trip, BookingRequest Request, Quote? Quote, string Boarding);

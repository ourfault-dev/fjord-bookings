using FjordBookings.Bookings;
using FjordBookings.Pricing;
using FjordBookings.Telemetry;
using FjordBookings.Trips;
using FjordBookings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace FjordBookings.Tests;

public sealed class BookingFormTests
{
    private static readonly DateOnly Today = new(2026, 6, 1);

    private static BookingForm Form(string trip = "LYSE", string date = "2026-06-14", string passengers = "3", string name = "Ola Nordmann", string email = "ola@example.no") =>
        BookingForm.From(new FormCollection(new Dictionary<string, StringValues>
        {
            ["trip"] = trip,
            ["date"] = date,
            ["passengers"] = passengers,
            ["name"] = name,
            ["email"] = email,
        }));

    [Fact]
    public void A_complete_form_makes_a_request()
    {
        var request = Form().Validate(new TripCatalogue(), Today);

        Assert.Equal(new BookingRequest("LYSE", new DateOnly(2026, 6, 14), 3, "Ola Nordmann", "ola@example.no"), request);
    }

    [Theory]
    [InlineData("ATLANTIS", "2026-06-14", "3", "Ola", "ola@example.no", "trip")]
    [InlineData("LYSE", "14/06/2026", "3", "Ola", "ola@example.no", "date")]
    [InlineData("LYSE", "2026-05-31", "3", "Ola", "ola@example.no", "date")]
    [InlineData("LYSE", "2026-06-14", "0", "Ola", "ola@example.no", "passengers")]
    [InlineData("LYSE", "2026-06-14", "13", "Ola", "ola@example.no", "passengers")]
    [InlineData("LYSE", "2026-06-14", "3", "O", "ola@example.no", "name")]
    [InlineData("LYSE", "2026-06-14", "3", "Ola", "ola", "email")]
    public void A_wrong_field_is_named(string trip, string date, string passengers, string name, string email, string field)
    {
        var form = Form(trip, date, passengers, name, email);

        Assert.Null(form.Validate(new TripCatalogue(), Today));
        Assert.Equal([field], form.Errors.Keys);
    }
}

public sealed class PageRenderingTests
{
    [Fact]
    public void Confirmation_shows_the_reference_sailing_and_total()
    {
        var trip = new TripCatalogue().Find("NAEROY")!;
        var request = new BookingRequest("NAEROY", new DateOnly(2026, 7, 9), 2, "Kari <Nordmann>", "kari@example.no");
        var confirmation = new Confirmation("NAE-0709-K7Q2", trip, request, new Quote(1026m, 2, 2052m), trip.Quay);

        var html = Pages.Confirmed(confirmation);

        Assert.Contains("NAE-0709-K7Q2", html);
        Assert.Contains("Kari &lt;Nordmann&gt;", html);
        Assert.Contains("Thursday 9 July 2026 at 10:30", html);
        Assert.Contains("NOK 2 052", html);
        Assert.Contains("Flåm harbour, berth 2", html);
    }

    [Fact]
    public void Booking_reference_names_the_trip_and_day()
    {
        var reference = BookingReference.New(new TripCatalogue().Find("GEIRANGER")!, new DateOnly(2026, 8, 3));

        Assert.Matches("^GEI-0803-[A-Z2-9]{4}$", reference);
    }
}

public sealed class OurfaultEnvironmentTests
{
    private readonly Dictionary<string, string> _environment = [];

    private Task Apply(Func<string, Task<string>>? readSecret = null) =>
        OurfaultEnvironment.ApplyAsync(
            name => _environment.GetValueOrDefault(name),
            (name, value) => _environment[name] = value,
            readSecret ?? (_ => throw new InvalidOperationException("No secret should be read.")));

    [Fact]
    public async Task The_token_becomes_the_three_exporter_headers()
    {
        _environment["OURFAULT_TOKEN"] = "ob_abc";

        await Apply();

        foreach (var name in OurfaultEnvironment.HeaderVariables)
        {
            Assert.Equal("Authorization=Bearer%20ob_abc", _environment[name]);
        }
    }

    [Fact]
    public async Task Without_the_token_the_secret_is_read()
    {
        _environment["OURFAULT_TOKEN_SECRET_ID"] = "demo/ourfault-token";
        string? asked = null;

        await Apply(id =>
        {
            asked = id;
            return Task.FromResult("ob_fromsecret\n");
        });

        Assert.Equal("demo/ourfault-token", asked);
        Assert.Equal("ob_fromsecret", _environment["OURFAULT_TOKEN"]);
        Assert.Equal("Authorization=Bearer%20ob_fromsecret", _environment["OTEL_EXPORTER_OTLP_HEADERS"]);
    }

    [Fact]
    public async Task A_header_already_set_is_kept()
    {
        _environment["OURFAULT_TOKEN"] = "ob_abc";
        _environment["OTEL_EXPORTER_OTLP_HEADERS"] = "Authorization=Bearer%20other";

        await Apply();

        Assert.Equal("Authorization=Bearer%20other", _environment["OTEL_EXPORTER_OTLP_HEADERS"]);
    }

    [Fact]
    public async Task Without_a_token_nothing_is_set()
    {
        await Apply();

        Assert.Empty(_environment);
    }
}

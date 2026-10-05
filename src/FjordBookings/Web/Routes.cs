using FjordBookings.Bookings;
using FjordBookings.Trips;

namespace FjordBookings.Web;

public static class Routes
{
    public static WebApplication MapSite(this WebApplication app)
    {
        app.MapGet("/", (TripCatalogue trips, SiteScripts scripts, string? variant) =>
            Html.Page("Boat trips on the western fjords", Pages.Home(trips, variant == "js2" ? scripts.Module("availability") : null)));

        app.MapGet("/book", (TripCatalogue trips, TimeProvider time, string? trip, int? variant) =>
            Html.Page("Book a trip", Pages.Book(trips, new BookingForm { Trip = trip ?? "" }, variant, Today(time))));

        app.MapPost("/book", async (HttpRequest request, TripCatalogue trips, BookingService bookings, TimeProvider time, int? variant) =>
        {
            var form = BookingForm.From(await request.ReadFormAsync());
            var today = Today(time);
            if (form.Validate(trips, today) is not { } booking)
            {
                return Html.Page("Book a trip", Pages.Book(trips, form, variant, today), StatusCodes.Status400BadRequest);
            }
            var confirmation = bookings.Book(booking, variant ?? 1);
            return Html.Page("Booking confirmed", Pages.Confirmed(confirmation));
        });

        app.MapGet("/healthz", () => Results.Text("ok"));

        app.MapFallback(() => Html.Page("Not found", Pages.NotFound(), StatusCodes.Status404NotFound));
        return app;
    }

    private static DateOnly Today(TimeProvider time) => DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
}

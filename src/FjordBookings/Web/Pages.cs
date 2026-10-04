using System.Globalization;
using System.Text;
using FjordBookings.Bookings;
using FjordBookings.Trips;

namespace FjordBookings.Web;

/// <summary>The site's pages, as HTML bodies inside <see cref="Html.Layout"/>.</summary>
public static class Pages
{
    /// <summary>An amount in whole kroner with a space between thousands, as in NOK 2 052.</summary>
    public static string Kroner(decimal amount) => "NOK " + amount.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ');

    public static string Hours(TimeSpan duration) =>
        duration.Minutes == 0 ? $"{duration.Hours} h" : $"{duration.Hours} h {duration.Minutes} min";

    public static string Home(TripCatalogue trips)
    {
        var cards = new StringBuilder();
        foreach (var trip in trips.All)
        {
            cards.Append($$"""
                <article class="trip">
                  <p class="fjord">{{Html.E(trip.Fjord)}}</p>
                  <h3>{{Html.E(trip.Name)}}</h3>
                  <p>{{Html.E(trip.Summary)}}</p>
                  <dl class="facts">
                    <div><dt>Departs</dt><dd>{{trip.Departs:HH\:mm}}</dd></div>
                    <div><dt>Duration</dt><dd>{{Hours(trip.Duration)}}</dd></div>
                    <div><dt>From</dt><dd>{{Kroner(trip.BasePrice)}}</dd></div>
                  </dl>
                  <a class="button" href="/book?trip={{Html.E(trip.Code)}}">Book this trip</a>
                </article>
                """);
        }
        return $$"""
            <section class="hero">
              <p class="eyebrow">Daily sailings, May to September</p>
              <h1>Quiet water, steep walls, early light.</h1>
              <p class="lede">Three small-boat trips on the fjords of western Norway, skippered by people who grew up on them. Twelve passengers or fewer to a booking, blankets on board.</p>
              <a class="button" href="#trips">See the trips</a>
            </section>
            <section id="trips" class="trips" aria-label="Trips">
            {{cards}}
            </section>
            """;
    }

    public static string Book(TripCatalogue trips, BookingForm form, int? variant, DateOnly today)
    {
        var options = new StringBuilder();
        foreach (var trip in trips.All)
        {
            var selected = string.Equals(trip.Code, form.Trip, StringComparison.OrdinalIgnoreCase) ? " selected" : "";
            options.Append($"""<option value="{Html.E(trip.Code)}"{selected}>{Html.E(trip.Name)} · {trip.Departs:HH\:mm}</option>""");
        }
        var chosen = trips.Find(form.Trip) ?? trips.All[0];
        var action = variant is { } v ? $"/book?variant={v}" : "/book";
        var summary = form.Errors.Count > 0
            ? """<p class="form-error" role="alert">Some details need another look before we can hold your seats.</p>"""
            : "";
        return $$"""
            <section class="booking">
              <div class="booking-form">
                <p class="eyebrow">Book a trip</p>
                <h1>Hold your seats</h1>
                {{summary}}
                <form method="post" action="{{Html.E(action)}}" novalidate>
                  {{Field("trip", "Trip", $"""<select id="trip" name="trip" required>{options}</select>""", form)}}
                  <div class="row">
                    {{Field("date", "Date", $"""<input id="date" name="date" type="date" required min="{today:yyyy-MM-dd}" value="{Html.E(form.Date)}">""", form)}}
                    {{Field("passengers", "Passengers", $"""<input id="passengers" name="passengers" type="number" required min="1" max="{BookingForm.MaxPassengers}" value="{Html.E(form.Passengers)}">""", form)}}
                  </div>
                  {{Field("name", "Name on the booking", $"""<input id="name" name="name" type="text" required autocomplete="name" value="{Html.E(form.Name)}">""", form)}}
                  {{Field("email", "Email for the tickets", $"""<input id="email" name="email" type="email" required autocomplete="email" value="{Html.E(form.Email)}">""", form)}}
                  <button class="button" type="submit">Confirm booking</button>
                  <p class="fine">You pay on board. Cancel free of charge up to 24 hours before sailing.</p>
                </form>
              </div>
              <aside class="booking-trip">
                <p class="fjord">{{Html.E(chosen.Fjord)}}</p>
                <h2>{{Html.E(chosen.Name)}}</h2>
                <p>{{Html.E(chosen.Summary)}}</p>
                <dl class="facts">
                  <div><dt>Boarding</dt><dd>{{Html.E(chosen.Quay)}}</dd></div>
                  <div><dt>Departs</dt><dd>{{chosen.Departs:HH\:mm}}</dd></div>
                  <div><dt>Duration</dt><dd>{{Hours(chosen.Duration)}}</dd></div>
                  <div><dt>Base fare</dt><dd>{{Kroner(chosen.BasePrice)}} per passenger</dd></div>
                </dl>
              </aside>
            </section>
            """;
    }

    private static string Field(string name, string label, string control, BookingForm form)
    {
        var error = form.Errors.TryGetValue(name, out var message)
            ? $"""<span class="field-error">{Html.E(message)}</span>"""
            : "";
        var invalid = error.Length > 0 ? " invalid" : "";
        return $"""<label class="field{invalid}" for="{name}"><span>{label}</span>{control}{error}</label>""";
    }

    public static string Confirmed(Confirmation confirmation)
    {
        var price = confirmation.Quote is { } quote
            ? $"""<div><dt>Total</dt><dd>{Kroner(quote.Total)} <span class="muted">({quote.Passengers} × {Kroner(quote.PerPassenger)})</span></dd></div>"""
            : "";
        return $$"""
            <section class="confirmation">
              <p class="eyebrow">Booking confirmed</p>
              <h1>See you on the water, {{Html.E(confirmation.Request.Name)}}.</h1>
              <p class="reference">Reference <strong>{{Html.E(confirmation.Reference)}}</strong></p>
              <dl class="facts wide">
                <div><dt>Trip</dt><dd>{{Html.E(confirmation.Trip.Name)}}</dd></div>
                <div><dt>Sailing</dt><dd>{{confirmation.Request.Date.ToString("dddd d MMMM yyyy", CultureInfo.InvariantCulture)}} at {{confirmation.Trip.Departs:HH\:mm}}</dd></div>
                <div><dt>Boarding</dt><dd>{{Html.E(confirmation.Boarding)}}</dd></div>
                <div><dt>Passengers</dt><dd>{{confirmation.Request.Passengers}}</dd></div>
                {{price}}
              </dl>
              <p>Your tickets are on their way to <strong>{{Html.E(confirmation.Request.Email)}}</strong>. Please be at the quay fifteen minutes before departure.</p>
              <a class="button secondary" href="/">Back to the trips</a>
            </section>
            """;
    }

    public static string Error() => """
        <section class="notice">
          <p class="eyebrow">Error 500</p>
          <h1>Something went wrong</h1>
          <p>We could not finish that request. Nothing has been booked or charged. Please try again in a little while.</p>
          <a class="button secondary" href="/">Back to the trips</a>
        </section>
        """;

    public static string NotFound() => """
        <section class="notice">
          <p class="eyebrow">Error 404</p>
          <h1>That page has drifted off</h1>
          <p>There is nothing at this address.</p>
          <a class="button secondary" href="/">Back to the trips</a>
        </section>
        """;
}

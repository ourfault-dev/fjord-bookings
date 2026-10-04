using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace FjordBookings.Web;

/// <summary>The site's one layout and HTML encoding. Every value that came from a request goes through <see cref="E"/>.</summary>
public static class Html
{
    public const string ContentType = "text/html; charset=utf-8";

    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    public static string E(string? value) => Encoder.Encode(value ?? "");

    public static IResult Page(string title, string body, int statusCode = StatusCodes.Status200OK) =>
        Results.Content(Layout(title, body), ContentType, Encoding.UTF8, statusCode);

    public static string Layout(string title, string body) => $$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{{E(title)}} · Fjord Bookings</title>
          <link rel="stylesheet" href="/site.css">
          <link rel="icon" href="/favicon.svg" type="image/svg+xml">
        </head>
        <body>
          <header class="masthead">
            <a class="brand" href="/">
              <svg viewBox="0 0 32 32" aria-hidden="true"><path d="M4 21h24l-4 5H8z" /><path d="M15 4v15M15 5l9 12h-9" /></svg>
              <span>Fjord Bookings</span>
            </a>
            <nav><a href="/#trips">Trips</a><a href="/book">Book a trip</a></nav>
          </header>
          <main>
        {{body}}
          </main>
          <footer class="footer">
            <p>Fjord Bookings · Small boats on the western fjords</p>
            <p class="note">A demonstration site. No bookings are taken and nothing is charged.</p>
          </footer>
        </body>
        </html>
        """;
}

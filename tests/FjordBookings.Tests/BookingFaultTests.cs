using System.Net;

namespace FjordBookings.Tests;

/// <summary>
/// Submitting a booking fails: the standard path in the pricing, and the two demonstration variants each in a class of
/// its own, every one with its own exception type, logged with the exception attached and answered with the short 500
/// page.
/// </summary>
public sealed class BookingFaultTests
{
    [Theory]
    [InlineData("/book", typeof(KeyNotFoundException), "SeasonalRates.MultiplierFor", "SeasonalRates.cs")]
    [InlineData("/book?variant=2", typeof(InvalidOperationException), "DepartureSchedule.DepartureOn", "DepartureSchedule.cs")]
    [InlineData("/book?variant=3", typeof(ArgumentOutOfRangeException), "FleetAllocation.BoatFor", "FleetAllocation.cs")]
    public async Task Submitting_a_booking_answers_500_and_logs_the_exception(string path, Type exceptionType, string frame, string file)
    {
        using var site = new Site();

        var response = await site.Browser().PostAsync(path, Site.Booking(), TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Something went wrong", html);
        Assert.DoesNotContain(exceptionType.Name, html);
        Assert.DoesNotContain("Stack", html);

        var logged = Assert.Single(site.Logs.Exceptions);
        Assert.Equal("FjordBookings.Web.ErrorPage", logged.Category);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Error, logged.Level);
        Assert.IsType(exceptionType, logged.Exception);
        Assert.Contains(frame, logged.Exception!.StackTrace);
        Assert.Contains(file + ":line ", logged.Exception.StackTrace);
        Assert.Contains("BookingService.Book", logged.Exception.StackTrace);
    }

    [Fact]
    public async Task Every_trip_fails_the_same_way()
    {
        using var site = new Site();

        foreach (var trip in new[] { "GEIRANGER", "NAEROY", "LYSE" })
        {
            var response = await site.Browser().PostAsync("/book", Site.Booking(trip), TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        Assert.Equal(3, site.Logs.Exceptions.Count);
        Assert.All(site.Logs.Exceptions, entry => Assert.IsType<KeyNotFoundException>(entry.Exception));
    }

    [Fact]
    public async Task In_Lambda_the_telemetry_is_flushed_and_the_pages_still_answer()
    {
        using var site = new Site { Settings = new Dictionary<string, string> { ["AWS_LAMBDA_FUNCTION_NAME"] = "fjord-bookings" } };
        var browser = site.Browser();

        var home = await browser.GetAsync("/", TestContext.Current.CancellationToken);
        var submit = await browser.PostAsync("/book", Site.Booking(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, submit.StatusCode);
        Assert.IsType<KeyNotFoundException>(Assert.Single(site.Logs.Exceptions).Exception);
    }
}

using System.Net;

namespace FjordBookings.Tests;

public sealed class PageTests(Site site) : IClassFixture<Site>
{
    [Fact]
    public async Task Landing_page_lists_the_three_trips()
    {
        var response = await site.Browser().GetAsync("/", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Geirangerfjord at first light", html);
        Assert.Contains("Nærøyfjord, narrow and quiet", html);
        Assert.Contains("Lysefjord under Pulpit Rock", html);
        Assert.Equal(3, html.Split("href=\"/book?trip=").Length - 1);
    }

    [Fact]
    public async Task Booking_page_shows_the_form_with_the_trip_chosen()
    {
        var response = await site.Browser().GetAsync("/book?trip=LYSE", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<form method=\"post\" action=\"/book\"", html);
        foreach (var field in new[] { "trip", "date", "passengers", "name", "email" })
        {
            Assert.Contains($"name=\"{field}\"", html);
        }
        Assert.Contains("<option value=\"LYSE\" selected>", html);
    }

    [Fact]
    public async Task Booking_page_keeps_the_variant_in_the_form_action()
    {
        var html = await site.Browser().GetStringAsync("/book?variant=2", TestContext.Current.CancellationToken);

        Assert.Contains("action=\"/book?variant=2\"", html);
    }

    [Fact]
    public async Task An_incomplete_booking_is_shown_again_with_what_is_missing()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["trip"] = "NAEROY", ["passengers"] = "40" });

        var response = await site.Browser().PostAsync("/book", form, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Pick the day you would like to sail.", html);
        Assert.Contains("Between 1 and 12 passengers", html);
        Assert.Contains("<option value=\"NAEROY\" selected>", html);
    }

    [Fact]
    public async Task Health_check_answers_200()
    {
        var response = await site.Browser().GetAsync("/healthz", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Stylesheet_is_served()
    {
        var response = await site.Browser().GetAsync("/site.css", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Unknown_address_is_the_404_page()
    {
        var response = await site.Browser().GetAsync("/cabins", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("That page has drifted off", html);
    }
}

using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace FjordBookings.Tests;

public sealed class BrowserScriptTests
{
    private const string Tag = "<script src=\"https://app.ourfault.dev/browser/v1.js\" data-key=\"pk_0123456789abcdef0123456789abcdef\" async></script>";

    private static Site WithSettings(params (string Key, string Value)[] settings) =>
        new() { Settings = settings.ToDictionary(setting => setting.Key, setting => setting.Value) };

    [Fact]
    public async Task Layout_has_no_script_tag_without_a_key()
    {
        using var site = new Site();

        var html = await site.Browser().GetStringAsync("/", TestContext.Current.CancellationToken);

        Assert.DoesNotContain("browser/v1.js", html);
    }

    [Fact]
    public async Task Layout_has_the_script_tag_with_the_key_on_every_page_when_one_is_set()
    {
        using var site = WithSettings(("OURFAULT_BROWSER_KEY", "pk_0123456789abcdef0123456789abcdef"));

        foreach (var path in new[] { "/", "/book", "/cabins" })
        {
            var html = await (await site.Browser().GetAsync(path, TestContext.Current.CancellationToken)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains(Tag, html);
        }
    }

    [Fact]
    public async Task Layout_names_the_endpoint_when_one_is_set()
    {
        using var site = WithSettings(("OURFAULT_BROWSER_KEY", "pk_abc"), ("OURFAULT_BROWSER_ENDPOINT", "http://127.0.0.1:5191"));

        var html = await site.Browser().GetStringAsync("/", TestContext.Current.CancellationToken);

        Assert.Contains("data-key=\"pk_abc\" data-endpoint=\"http://127.0.0.1:5191\" async></script>", html);
    }

    [Fact]
    public async Task A_blank_key_leaves_the_tag_out()
    {
        using var site = WithSettings(("OURFAULT_BROWSER_KEY", "  "));

        var html = await site.Browser().GetStringAsync("/", TestContext.Current.CancellationToken);

        Assert.DoesNotContain("browser/v1.js", html);
    }

    [Fact]
    public async Task The_error_page_carries_the_tag_too()
    {
        using var site = WithSettings(("OURFAULT_BROWSER_KEY", "pk_0123456789abcdef0123456789abcdef"));

        var response = await site.Browser().PostAsync("/book", Site.Booking(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains(Tag, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Every_page_has_the_newsletter_form_with_named_controls_and_its_module()
    {
        using var site = new Site();

        foreach (var path in new[] { "/", "/book", "/cabins" })
        {
            var html = await (await site.Browser().GetAsync(path, TestContext.Current.CancellationToken)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("<form id=\"newsletter\"", html);
            Assert.Contains("name=\"email\" type=\"email\" aria-label=\"Email address for the newsletter\"", html);
            Assert.Contains("name=\"subscribe\" aria-label=\"Subscribe to the newsletter\"", html);
            Assert.Matches(@"<script type=""module"" src=""/js/newsletter\.[0-9a-f]{8}\.js""></script>", html);
        }
    }

    [Fact]
    public async Task The_availability_button_is_only_on_the_landing_page_with_variant_js2()
    {
        using var site = new Site();
        var client = site.Browser();

        var plain = await client.GetStringAsync("/", TestContext.Current.CancellationToken);
        var variant = await client.GetStringAsync("/?variant=js2", TestContext.Current.CancellationToken);

        Assert.DoesNotContain("check-availability", plain);
        Assert.DoesNotContain("availability.", plain);
        Assert.Contains("aria-label=\"Check availability\"", variant);
        Assert.Matches(@"<script type=""module"" src=""/js/availability\.[0-9a-f]{8}\.js""></script>", variant);
    }

    [Theory]
    [InlineData("newsletter")]
    [InlineData("availability")]
    public async Task Modules_are_served_as_javascript_cached_for_a_year(string name)
    {
        using var site = new Site();
        var client = site.Browser();
        var page = await client.GetStringAsync(name == "newsletter" ? "/" : "/?variant=js2", TestContext.Current.CancellationToken);
        var path = Assert.Single(ModulePath(name).Matches(page)).Groups["path"].Value;

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/javascript", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("public, max-age=31536000, immutable", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Stylesheet_is_not_cached_as_immutable()
    {
        using var site = new Site();

        var response = await site.Browser().GetAsync("/site.css", TestContext.Current.CancellationToken);

        Assert.DoesNotContain("immutable", response.Headers.CacheControl?.ToString() ?? "");
    }

    [Fact]
    public void Module_file_names_carry_the_hash_of_their_content()
    {
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "../../../../../src/FjordBookings/wwwroot/js");
        var files = Directory.GetFiles(directory, "*.js");

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)))[..8];
            Assert.True(
                Path.GetFileName(file).EndsWith($".{hash}.js", StringComparison.Ordinal),
                $"{Path.GetFileName(file)} should be renamed to carry {hash}, the first eight hex characters of its SHA-256.");
        }
    }

    private static Regex ModulePath(string name) => new($@"src=""(?<path>/js/{name}\.[0-9a-f]{{8}}\.js)""");
}

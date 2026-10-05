using System.Text.RegularExpressions;

namespace FjordBookings.Web;

/// <summary>
/// The scripts the pages carry: ourfault's browser script when <c>OURFAULT_BROWSER_KEY</c> is set, and the site's own
/// modules under <c>wwwroot/js</c>, whose file names carry the first eight hex characters of their content's SHA-256
/// (<c>newsletter.53205312.js</c>) and are cached for a year.
/// </summary>
public sealed partial class SiteScripts
{
    public const string KeyVariable = "OURFAULT_BROWSER_KEY";
    public const string EndpointVariable = "OURFAULT_BROWSER_ENDPOINT";
    public const string ScriptUrl = "https://app.ourfault.dev/browser/v1.js";
    public const string ModulePrefix = "/js/";

    private readonly Dictionary<string, string> _modules;

    public SiteScripts(IConfiguration configuration, IWebHostEnvironment environment)
    {
        BrowserKey = Clean(configuration[KeyVariable]);
        BrowserEndpoint = Clean(configuration[EndpointVariable]);
        _modules = [];
        foreach (var file in environment.WebRootFileProvider.GetDirectoryContents("js"))
        {
            if (HashedName().Match(file.Name) is { Success: true } match)
            {
                _modules[match.Groups["name"].Value] = ModulePrefix + file.Name;
            }
        }
    }

    /// <summary>The public browser key, or null when none is configured and the tag is left out.</summary>
    public string? BrowserKey { get; }

    /// <summary>The ingest base URL for <c>data-endpoint</c>, set only to send somewhere other than ourfault's ingest.</summary>
    public string? BrowserEndpoint { get; }

    /// <summary>The path of the module <paramref name="name"/> with its content hash in the file name.</summary>
    public string Module(string name) =>
        _modules.TryGetValue(name, out var path) ? path : throw new InvalidOperationException($"No module {name}.<hash>.js in wwwroot/js.");

    public string BrowserTag()
    {
        if (BrowserKey is not { } key)
        {
            return "";
        }
        var endpoint = BrowserEndpoint is { } url ? $" data-endpoint=\"{Html.E(url)}\"" : "";
        return $"""<script src="{ScriptUrl}" data-key="{Html.E(key)}"{endpoint} async></script>""";
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex(@"^(?<name>[a-z]+)\.[0-9a-f]{8}\.js$")]
    private static partial Regex HashedName();
}

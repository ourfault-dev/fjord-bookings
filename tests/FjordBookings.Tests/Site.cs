using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace FjordBookings.Tests;

/// <summary>A log record as the app wrote it.</summary>
public sealed record LogEntry(string Category, LogLevel Level, string Message, Exception? Exception);

/// <summary>Keeps every record the app logs, so a test can read the exception the logs exporter would send.</summary>
public sealed class CapturedLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => [.. _entries];

    public IReadOnlyList<LogEntry> Exceptions => [.. _entries.Where(entry => entry.Exception is not null)];

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class Logger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue(new LogEntry(category, logLevel, formatter(state, exception), exception));
    }
}

/// <summary>The site in memory, with its logs captured and, optionally, extra configuration.</summary>
public sealed class Site : WebApplicationFactory<Program>
{
    /// <summary>Configuration the app reads as if from its environment.</summary>
    public IReadOnlyDictionary<string, string> Settings { get; init; } = new Dictionary<string, string>();

    public CapturedLogs Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var (key, value) in Settings)
        {
            builder.UseSetting(key, value);
        }
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
    }

    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>A valid booking for a day a month from now.</summary>
    public static FormUrlEncodedContent Booking(string trip = "GEIRANGER") => new(new Dictionary<string, string>
    {
        ["trip"] = trip,
        ["date"] = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        ["passengers"] = "2",
        ["name"] = "Ingrid Solberg",
        ["email"] = "ingrid@example.com",
    });
}

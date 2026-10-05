using FjordBookings.Bookings;
using FjordBookings.Departures;
using FjordBookings.Fleet;
using FjordBookings.Pricing;
using FjordBookings.Telemetry;
using FjordBookings.Trips;
using FjordBookings.Web;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

await OurfaultEnvironment.ApplyAsync();

var builder = WebApplication.CreateBuilder(args);

// Serves API Gateway's HTTP API events when the app runs in Lambda; does nothing elsewhere.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

builder.Services.AddOpenTelemetry()
    .WithLogging(logging => logging.AddOtlpExporter())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("System.Runtime") // .NET 9 and later: memory, CPU and GC
        .AddOtlpExporter());
builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("*", LogLevel.Warning);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<SiteScripts>();
builder.Services.AddSingleton<TripCatalogue>();
builder.Services.AddSingleton<SeasonalRates>();
builder.Services.AddSingleton<PricingService>();
builder.Services.AddSingleton<DepartureSchedule>();
builder.Services.AddSingleton<FleetAllocation>();
builder.Services.AddSingleton<BookingService>();
builder.Services.AddExceptionHandler<ErrorPage>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (LambdaTelemetryFlush.InLambda(app.Configuration))
{
    app.UseMiddleware<LambdaTelemetryFlush>();
}
app.UseExceptionHandler();
app.UseStaticFiles(new StaticFileOptions
{
    // The modules' names carry their content hash, so a browser may keep one for as long as it likes.
    OnPrepareResponse = context =>
    {
        if (context.Context.Request.Path.StartsWithSegments("/js"))
        {
            context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
    },
});
app.MapSite();

app.Run();

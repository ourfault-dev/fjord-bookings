using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace FjordBookings.Telemetry;

/// <summary>
/// In AWS Lambda, sends what the OpenTelemetry providers hold before the response leaves, since Lambda freezes the
/// process between invocations and a frozen process sends nothing. The request's own span ends after this runs and
/// leaves with the next invocation's flush.
/// </summary>
public sealed class LambdaTelemetryFlush(RequestDelegate next, TracerProvider tracer, MeterProvider meter, LoggerProvider logger)
{
    private const int TimeoutMilliseconds = 2000;

    /// <summary>Whether the process runs in Lambda, which sets <c>AWS_LAMBDA_FUNCTION_NAME</c>.</summary>
    public static bool InLambda(IConfiguration configuration) =>
        !string.IsNullOrEmpty(configuration["AWS_LAMBDA_FUNCTION_NAME"]);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        finally
        {
            await Task.WhenAll(
                Task.Run(() => logger.ForceFlush(TimeoutMilliseconds)),
                Task.Run(() => tracer.ForceFlush(TimeoutMilliseconds)),
                Task.Run(() => meter.ForceFlush(TimeoutMilliseconds)));
        }
    }
}

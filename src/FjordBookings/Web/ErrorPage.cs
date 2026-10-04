using Microsoft.AspNetCore.Diagnostics;

namespace FjordBookings.Web;

/// <summary>
/// Answers an unhandled exception with the short 500 page, in every environment, and logs it with the exception
/// attached, which is the record the OpenTelemetry logs exporter sends.
/// </summary>
public sealed class ErrorPage(ILogger<ErrorPage> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Request {Method} {Path} failed", httpContext.Request.Method, httpContext.Request.Path.Value);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = Html.ContentType;
        await httpContext.Response.WriteAsync(Html.Layout("Something went wrong", Pages.Error()), cancellationToken);
        return true;
    }
}

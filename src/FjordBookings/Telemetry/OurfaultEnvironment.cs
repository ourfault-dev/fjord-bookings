namespace FjordBookings.Telemetry;

/// <summary>
/// Puts the ourfault ingest token where the OpenTelemetry exporters read it: the three
/// <c>OTEL_EXPORTER_OTLP_HEADERS</c> variable of the ourfault setup. The token is <c>OURFAULT_TOKEN</c>, or, when
/// that is unset, the value of the Secrets Manager secret named by <c>OURFAULT_TOKEN_SECRET_ID</c>, read once at start-up.
/// A header variable that is already set is left as it is. Without a token nothing is set and nothing is exported.
/// </summary>
public static class OurfaultEnvironment
{
    public const string TokenVariable = "OURFAULT_TOKEN";
    public const string TokenSecretVariable = "OURFAULT_TOKEN_SECRET_ID";

    // The .NET exporter reads the base OTLP variables; the per-signal ones are not applied.
    public static readonly IReadOnlyList<string> HeaderVariables = ["OTEL_EXPORTER_OTLP_HEADERS"];

    public static string Header(string token) => "Authorization=Bearer%20" + token;

    public static async Task ApplyAsync(Func<string, string?> get, Action<string, string> set, Func<string, Task<string>> readSecret)
    {
        var token = get(TokenVariable);
        if (string.IsNullOrWhiteSpace(token) && get(TokenSecretVariable) is { Length: > 0 } secretId)
        {
            token = (await readSecret(secretId)).Trim();
            set(TokenVariable, token);
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }
        foreach (var name in HeaderVariables)
        {
            if (string.IsNullOrEmpty(get(name)))
            {
                set(name, Header(token.Trim()));
            }
        }
    }

    /// <summary>Applies the token to this process's environment.</summary>
    public static Task ApplyAsync() =>
        ApplyAsync(Environment.GetEnvironmentVariable, Environment.SetEnvironmentVariable, SecretsManagerToken.ReadAsync);
}

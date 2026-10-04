using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace FjordBookings.Telemetry;

/// <summary>Reads the ingest token from Secrets Manager with the function's own credentials.</summary>
public static class SecretsManagerToken
{
    public static async Task<string> ReadAsync(string secretId)
    {
        using var client = new AmazonSecretsManagerClient();
        var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId });
        return response.SecretString;
    }
}

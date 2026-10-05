using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;
using Distribution = Amazon.CDK.AWS.CloudFront.Distribution;
using Function = Amazon.CDK.AWS.Lambda.Function;
using FunctionProps = Amazon.CDK.AWS.Lambda.FunctionProps;

namespace FjordBookings.Infra;

public sealed class SiteStackProps : StackProps
{
    /// <summary>The <c>dotnet publish</c> output of src/FjordBookings, relative to infra/.</summary>
    public required string AssetPath { get; init; }

    /// <summary>
    /// The us-east-1 ACM certificate for <see cref="SiteStack.Domain"/>, which the Deploy workflow requests and validates
    /// through Cloudflare. Without it the distribution answers on its cloudfront.net name only.
    /// </summary>
    public string? CertificateArn { get; init; }

    /// <summary>
    /// ourfault's public browser key for the site's pages, from the GitHub environment variable <c>OURFAULT_BROWSER_KEY</c>.
    /// Without it the pages carry no browser script.
    /// </summary>
    public string? BrowserKey { get; init; }

    /// <summary>The commit deployed, reported as the service's <c>service.version</c>.</summary>
    public required string CommitSha { get; init; }
}

/// <summary>
/// Fjord Bookings: the ASP.NET Core app on the managed .NET 10 runtime with SnapStart, behind an HTTP API and a
/// CloudFront distribution for <see cref="Domain"/>. The app sends its errors, traces and metrics to ourfault with the
/// ingest token in the Secrets Manager secret <see cref="TokenSecretName"/>, which the Deploy workflow writes.
/// </summary>
public sealed class SiteStack : Stack
{
    public const string Name = "fjord-bookings";
    public const string AccountId = "694165776402";
    public const string RegionName = "eu-north-1";
    public const string Domain = "demo.ourfault.dev";
    public const string TokenSecretName = "demo/ourfault-token";
    public const string ServiceName = "fjord-bookings";
    public const string Ingest = "https://ingest.ourfault.dev";

    public SiteStack(Construct scope, string id, SiteStackProps props)
        : base(scope, id, props)
    {
        var functionName = $"{StackName}-site";
        var token = Secret.FromSecretNameV2(this, "OurfaultToken", TokenSecretName);

        var function = new Function(this, "SiteFunction", new FunctionProps
        {
            FunctionName = functionName,
            // An executable assembly on the managed runtime: the handler is the assembly name, and
            // Amazon.Lambda.AspNetCoreServer.Hosting in the app takes the events from there.
            Runtime = Runtime.DOTNET_10,
            Architecture = Architecture.ARM_64,
            Handler = "FjordBookings",
            Code = Code.FromAsset(props.AssetPath),
            MemorySize = 1024,
            Timeout = Duration.Seconds(15),
            SnapStart = SnapStartConf.ON_PUBLISHED_VERSIONS,
            LogGroup = new LogGroup(this, "SiteLogGroup", new LogGroupProps
            {
                LogGroupName = $"/aws/lambda/{functionName}",
                Retention = RetentionDays.TWO_WEEKS,
                RemovalPolicy = RemovalPolicy.DESTROY,
            }),
            // The ourfault setup's environment: the base OTLP variables, which the .NET exporter reads (it ignores the
            // per-signal ones); the app reads the token from the secret at start-up and sets the headers variable from it.
            Environment = new Dictionary<string, string>
            {
                ["GIT_SHA"] = props.CommitSha,
                ["OTEL_SERVICE_NAME"] = ServiceName,
                ["OTEL_RESOURCE_ATTRIBUTES"] = $"service.version={props.CommitSha}",
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = Ingest,
                ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
                ["OURFAULT_TOKEN_SECRET_ID"] = TokenSecretName,
            },
        });
        if (props.BrowserKey is { } browserKey)
        {
            function.AddEnvironment("OURFAULT_BROWSER_KEY", browserKey);
        }
        _ = token.GrantRead(function);

        // SnapStart applies to published versions only, so the API invokes an alias on the current one.
        Annotations.Of(function).AcknowledgeWarning("@aws-cdk/aws-lambda:snapStartRequirePublish", "The API invokes the live alias on a published version.");
        var alias = new Alias(this, "SiteAlias", new AliasProps
        {
            AliasName = "live",
            Version = function.CurrentVersion,
        });

        var api = new HttpApi(this, "SiteApi", new HttpApiProps
        {
            ApiName = $"{StackName}-api",
            DefaultIntegration = new HttpLambdaIntegration("SiteIntegration", alias),
        });

        var certificate = props.CertificateArn is { } arn
            ? Certificate.FromCertificateArn(this, "Certificate", arn)
            : null;
        var distribution = new Distribution(this, "SiteDistribution", new DistributionProps
        {
            Comment = $"{Domain}, Fjord Bookings",
            DomainNames = certificate is null ? null : [Domain],
            Certificate = certificate,
            PriceClass = PriceClass.PRICE_CLASS_100,
            DefaultBehavior = new BehaviorOptions
            {
                Origin = new HttpOrigin(Fn.Select(2, Fn.Split("/", api.ApiEndpoint)), new HttpOriginProps
                {
                    ProtocolPolicy = OriginProtocolPolicy.HTTPS_ONLY,
                }),
                ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                AllowedMethods = AllowedMethods.ALLOW_ALL,
                CachePolicy = CachePolicy.CACHING_DISABLED,
                OriginRequestPolicy = OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER,
            },
        });

        _ = new CfnOutput(this, "DistributionDomainName", new CfnOutputProps
        {
            Description = $"The CloudFront name the {Domain} CNAME points at.",
            Value = distribution.DistributionDomainName,
        });
        _ = new CfnOutput(this, "DistributionId", new CfnOutputProps { Value = distribution.DistributionId });
        _ = new CfnOutput(this, "ApiEndpoint", new CfnOutputProps { Value = api.ApiEndpoint });
    }
}

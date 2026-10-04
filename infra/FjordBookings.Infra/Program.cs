using Amazon.CDK;
using FjordBookings.Infra;

var app = new App();

string? Context(string key) => app.Node.TryGetContext(key) is string { Length: > 0 } value ? value : null;

_ = new SiteStack(app, SiteStack.Name, new SiteStackProps
{
    Env = new Amazon.CDK.Environment { Account = SiteStack.AccountId, Region = SiteStack.RegionName },
    AssetPath = Context("assetPath") ?? "../out/fjord-bookings",
    CertificateArn = Context("certificateArn"),
    CommitSha = Context("commitSha") ?? "unknown",
});

app.Synth();

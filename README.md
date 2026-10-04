# Fjord Bookings

A small booking site for boat trips on the western fjords of Norway: a landing page with three trips, a booking form, and a confirmation page. It is an ASP.NET Core minimal API on .NET 10 that renders its HTML on the server, deployed to AWS Lambda behind CloudFront at https://demo.ourfault.dev.

It exists to demonstrate [ourfault](https://ourfault.dev), which reads a service's OpenTelemetry errors and files GitHub issues for the ones that are the code's fault. Submitting a booking always fails with a real exception from code in this repository, so ourfault has something to find. No bookings are taken.

## Run it

```bash
dotnet run --project src/FjordBookings     # http://localhost:5080
dotnet test
```

The site sends its logs, traces and metrics to ourfault with the setup from ourfault's Setup page, in `src/FjordBookings/Program.cs`. Locally it sends nothing unless the environment says where to:

```bash
export OURFAULT_TOKEN=<your token>
export OTEL_SERVICE_NAME=fjord-bookings
export OTEL_RESOURCE_ATTRIBUTES=service.version=${GIT_SHA}
export OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=https://ingest.ourfault.dev/v1/logs
export OTEL_EXPORTER_OTLP_LOGS_PROTOCOL=http/protobuf
export OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=https://ingest.ourfault.dev/v1/traces
export OTEL_EXPORTER_OTLP_TRACES_PROTOCOL=http/protobuf
export OTEL_EXPORTER_OTLP_METRICS_ENDPOINT=https://ingest.ourfault.dev/v1/metrics
export OTEL_EXPORTER_OTLP_METRICS_PROTOCOL=http/protobuf
```

At start-up the app turns `OURFAULT_TOKEN` into the three `OTEL_EXPORTER_OTLP_*_HEADERS` variables (`Authorization=Bearer%20<token>`). In Lambda the token comes from the Secrets Manager secret named by `OURFAULT_TOKEN_SECRET_ID`, and the requests' telemetry is flushed before each response leaves, since Lambda freezes the process between invocations.

## Demonstrating

Every valid booking answers a short "Something went wrong" page with status 500, and the exception is logged through `ILogger` with the exception attached, which the logs exporter sends to ourfault with its type and stack trace.

| Booking form | Exception | Where |
|---|---|---|
| `/book` | `KeyNotFoundException` | `SeasonalRates.MultiplierFor`, `src/FjordBookings/Pricing/SeasonalRates.cs` line 30: the month key is `"Jun"`, the table's keys are lower case |
| `/book?variant=2` | `InvalidOperationException` | `DepartureSchedule.DepartureOn`, `src/FjordBookings/Departures/DepartureSchedule.cs` line 25: a sailing's time is compared with midnight |
| `/book?variant=3` | `ArgumentOutOfRangeException` | `FleetAllocation.BoatFor`, `src/FjordBookings/Fleet/FleetAllocation.cs` line 22: `boats[boats.Count]` |

The variants are not linked from the site. Open the booking form with `?variant=2` or `?variant=3` and submit it, and each gives ourfault a new fingerprint from a different class. Each path goes through `BookingService.Book` (`src/FjordBookings/Bookings/BookingService.cs`) from the `POST /book` endpoint in `src/FjordBookings/Web/Routes.cs`. `GET /healthz` answers 200.

## Deploy

Every push to `main` runs `.github/workflows/deploy.yml` in the GitHub environment `demo`, against AWS account 694165776402 in eu-north-1:

1. Checks that the environment has `AWS_DEPLOY_ARN` (variable), `CLOUDFLARE_API_TOKEN` and `OURFAULT_TOKEN` (secrets), and stops if not.
2. Runs the tests and publishes the site, framework-dependent for linux-arm64, with its `.pdb` so stack traces carry file names and lines.
3. Writes `OURFAULT_TOKEN` into the secret `demo/ourfault-token`, creating it the first time.
4. Finds or requests the ACM certificate for `demo.ourfault.dev` in us-east-1, writes its validation CNAME to the `ourfault.dev` zone through the Cloudflare API, and waits until it is issued.
5. `cdk deploy fjord-bookings` from `infra/`: the Lambda function (`dotnet10`, arm64, SnapStart, alias `live`), an HTTP API, and a CloudFront distribution for `demo.ourfault.dev` with that certificate.
6. Writes the `demo` CNAME to the distribution's name.
7. Smoke test: `/` and `/healthz` answer 200, and a booking answers the 500 page.

DNS and the certificate are handled by workflow steps rather than a custom resource: CloudFormation's certificate resource does not finish until the certificate is validated and does not expose the validation record, so the workflow requests the certificate with the AWS CLI and writes the records with `.github/scripts/cloudflare-cname.sh`. Both records are DNS-only CNAMEs. `OWNER-STEPS.md` lists what the owner sets up once.

To synthesise the stack locally, publish first, since the stack packages `out/fjord-bookings`:

```bash
dotnet publish src/FjordBookings --runtime linux-arm64 --self-contained false --configuration Release --output out/fjord-bookings
cd infra && npx aws-cdk@2 synth
```

## Structure

```
src/FjordBookings/           the site: Program.cs, Web/ (pages, routes, error page), Bookings/, Pricing/, Departures/, Fleet/, Trips/, Telemetry/
tests/FjordBookings.Tests/   xunit v3 on Microsoft Testing Platform: pages, the faults, the form and the token setup
infra/                       the CDK app in C#
.github/                     the Deploy workflow and the Cloudflare record script
```

## Licence

MIT, see `LICENSE`.

# Owner steps

What the owner does once, in order, for https://demo.ourfault.dev. The repository, the deploy role `arn:aws:iam::694165776402:role/DemoDeployRole` and the GitHub environment `demo` with its variable `AWS_DEPLOY_ARN` already exist. The push of `main` starts the Deploy workflow, which stops at "Check the environment" until step 2 is done.

## 1. The workspace and its token

On https://app.ourfault.dev:

1. Choose **A new workspace** and name it `Fjord Bookings`.
2. If the workspace says it is not connected to GitHub, choose **Install the GitHub App** and connect the installation on `ourfault-dev` (step 3).
3. On the workspace's **Setup** page, choose **Create token**, name it `fjord-bookings`, and copy the token. It is shown once.
4. In **Workspace settings**, make sure **Propose fixes as draft pull requests** is off (the default), so the faults stay in place for the next demonstration.

## 2. The environment's secrets

From any shell signed in to GitHub, each command prompting for the value:

```bash
gh secret set OURFAULT_TOKEN --env demo --repo ourfault-dev/fjord-bookings          # the token from step 1
gh secret set CLOUDFLARE_API_TOKEN --env demo --repo ourfault-dev/fjord-bookings    # Zone:DNS:Edit on ourfault.dev only
```

The Cloudflare token can be the product's or a new one with `Zone:DNS:Edit` on the `ourfault.dev` zone. The workflow writes two records there: the certificate's `_….demo.ourfault.dev` validation CNAME and the `demo.ourfault.dev` CNAME, both DNS-only.

## 3. The GitHub App's repository access

On 2026-10-04 the App installations on `ourfault-dev` (`ourfault-dev` and `ourfault-dev-dev`) were on **All repositories**, so the App sees `fjord-bookings` and there is nothing to do. If an installation is moved to selected repositories: GitHub, `ourfault-dev` organisation, **Settings**, **GitHub Apps**, the App, **Configure**, **Repository access**, add `fjord-bookings`, **Save**. Without access ourfault can triage the failures but cannot read the code or file the issue.

## 4. The first deploy

Re-run the Deploy workflow that stopped at step 2, or start it:

```bash
gh workflow run Deploy --repo ourfault-dev/fjord-bookings --ref main
gh run watch --repo ourfault-dev/fjord-bookings
```

The first run requests the certificate and waits for ACM to issue it, a few minutes, then deploys the stack and points `demo.ourfault.dev` at CloudFront. A step that fails with AccessDenied means the deploy role lacks one of these, in 694165776402:

- `sts:AssumeRole` on the `cdk-hnb659fds-*` roles in eu-north-1 (the account is bootstrapped there for the product), and `cloudformation:DescribeStacks`;
- `secretsmanager:DescribeSecret`, `GetSecretValue`, `PutSecretValue` and `CreateSecret` on `demo/ourfault-token*`;
- `acm:ListCertificates`, `acm:RequestCertificate`, `acm:DescribeCertificate` and `acm:AddTagsToCertificate` in us-east-1.

## 5. The demonstration

1. Open https://demo.ourfault.dev, choose a trip, fill in the form and choose **Confirm booking**. The site answers "Something went wrong". The deploy's smoke test has already done this once.
2. In the workspace, open **Services and repositories**. `fjord-bookings` is suggested or listed under Unmapped: **Confirm** or **Map** it to `ourfault-dev/fjord-bookings`.
3. Submit a booking again. ourfault files an issue in `ourfault-dev/fjord-bookings` naming `SeasonalRates.MultiplierFor` in `src/FjordBookings/Pricing/SeasonalRates.cs`.
4. For a fresh failure, open https://demo.ourfault.dev/book?variant=2 or `?variant=3` and submit: an `InvalidOperationException` in `DepartureSchedule` or an `ArgumentOutOfRangeException` in `FleetAllocation`, each its own fingerprint (README, "Demonstrating").

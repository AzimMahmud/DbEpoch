# Security Policy

## Supported versions

| Version | Supported |
|---------|-----------|
| 2.x     | ✅ |
| 1.x     | ✅ |

## Reporting a vulnerability

We take security seriously. If you discover a security vulnerability in DbShift,
please **do not** open a public issue. Instead, report it privately:

- Open a [draft security advisory](https://github.com/AzimMahmud/dbshift/security/advisories/new)
- Or email the maintainers directly (check the commit history for contacts)

We will acknowledge receipt within 48 hours and provide a timeline for a fix.
Vulnerabilities will be disclosed after a fix is released.

## Scope

- The `dbshift` binary and its source code
- The install scripts (`install.sh`, `install.ps1`)
- The build/publish scripts
- The NuGet package (`DbShift` on [nuget.org](https://www.nuget.org/packages/DbShift))

## Out of scope

- User-authored SQL migration scripts (we validate syntax but not intent)
- Third-party NuGet packages (report to their respective maintainers)
- Database servers and infrastructure

## Security practices

- **No secrets in code.** Connection strings use environment variable expansion (`${VAR}`). No credentials are committed to the repository.
- **Supply chain.** Dependabot monitors NuGet and GitHub Actions dependencies weekly. CodeQL runs `security-and-quality` analysis on every push/PR to `main` and on a weekly schedule.
- **Build integrity.** CI builds run with `TreatWarningsAsErrors` (zero warnings). Release binaries are self-contained and deterministic (`ContinuousIntegrationBuild` is set in CI).
- **Locking.** Distributed locks prevent concurrent migrations to the same environment, avoiding race conditions during deploys.
- **Audit trail.** Every migration action is logged to an append-only `__migration_audit` table — who ran what, when, and the result.

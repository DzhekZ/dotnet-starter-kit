# Integration testing

`src/Tests/Integration.Tests/` + `Integration.Middleware.Tests/`. Read before writing tests that touch the DB/HTTP pipeline. See `testing.md` for unit conventions.

## Harness

`WebApplicationFactory` over **real** infra via Testcontainers — PostgreSQL + Redis + RustFS (S3-compatible, generic container). **Docker must be running**; if it isn't, tests fail fast with `DockerUnavailableException` (environmental, not a regression — run the unit projects instead).

`FshWebApplicationFactory` (`Integration.Tests/Infrastructure/`) boots the containers, overlays in-memory config, swaps `IMailService` → `NoOpMailService`, and rewires storage to RustFS.

## Must-know gotchas

- **Tenant context is AsyncLocal — set it inline.** Set the Finbuckle tenant context **in the same method** as the `UserManager`/`DbContext` call. Setting it in an awaited helper loses it across the async boundary → NRE in the tenant query filter.
- **Storage is wired eagerly.** `AddHeroStorage` reads `Storage:Provider` before the test config overlay, so it picks `LocalStorageService`. The factory **removes the `IStorageService`/`LocalStorageService`/`S3StorageService` descriptors post-registration and re-registers the S3 stack** at RustFS. Follow that when a test needs real object storage. (See `storage.md`.)
- **SignalR tests force long-polling** — TestServer has no WebSocket. Configure the client transport accordingly.
- **Rate limiting is read eagerly** — `Integration.Middleware.Tests` sets `RateLimitingOptions:Enabled` via env var **before** host build, since flipping it after has no effect.
- **Some production state is process-static and follows the most recently started host:** Hangfire's `JobStorage.Current` (`HangfireService` enqueues through it) and the static `Audit.Publisher` that `AuditHttpMiddleware` writes through (`AuditingConfigurator` re-points it at every host start). A `WithWebHostBuilder` host (webhook tests) builds its own storage/publisher and tears them down with the host, so without `FshWebApplicationFactory.PrimaryHostStaticsRestorer` every later test that creates a tenant dies with `ObjectDisposedException` (Hangfire.InMemory) and Activity audits are never persisted (`AuditQueryByKeyTests` times out) — and which tests run after the webhook tests depends on xUnit's ordering. Another static a new module sets at startup needs the same restore.
- **Trash/restore views must keep the tenant filter.** Cover them with a two-tenant test (`FileTenantIsolationTests`, `ChatTenantIsolationTests`): handlers lift only `QueryFilters.SoftDelete`; an unnamed `IgnoreQueryFilters()` also drops tenant scoping (see `database.md`).

## Coverage

```bash
dotnet test --collect "XPlat Code Coverage" --settings coverage.runsettings
```
Cobertura; includes `[FSH.Modules.*]` + `[FSH.Framework.*]`; excludes tests, the Migrations project, and `*HostedService`.

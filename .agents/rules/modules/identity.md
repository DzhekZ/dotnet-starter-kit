# Module: Identity

Auth (JWT + ASP.NET Identity), users, roles, permissions, sessions, impersonation, 2FA.

## Service shape

`IUserService` is a **facade** that delegates to focused single-responsibility services — change behavior in the specific service, not the facade:

| Interface | Concern |
|---|---|
| `IUserRegistrationService` | register, external-principal create, email/phone confirm |
| `IUserProfileService` | get/list/count, update profile, image, existence checks |
| `IUserStatusService` | activate/deactivate (`DeleteAsync` == deactivate), audited toggles |
| `IUserRoleService` | role assignment, admin-role guards |
| `IUserPasswordService` | forgot/reset/change password, history + expiry |
| `IUserPermissionService` | effective permissions, cache invalidation |

`ChangePassword`/`Update`/`Delete` etc. flow facade → service → EF/UserManager. `CancellationToken` is `= default` on these interfaces and propagated into EF sinks (note: `UserManager`/`RoleManager` have no CT overloads, so private helpers that only call them don't take one).

## Permission gating footgun

`RequiredPermissionAttribute` implements `FSH.Framework.Shared.Identity.Authorization.IRequiredPermissionMetadata`. **Never let a second/duplicate `IRequiredPermissionMetadata` appear** — it silently disables **all** `.RequirePermission()` gates across the app. Permission constants live in `Shared/Identity/*Permissions.cs`.

## Hosted services (background)

- `RolePermissionSyncHostedService` — best-effort sync of the permission catalog; loops, catches `Exception` *with* an `OperationCanceledException` filter, logs and continues.
  The sync is authoritative for the built-in `Basic` and `Admin` roles: it adds missing permissions **and removes revoked ones** (root `Admin` keeps `Root` permissions), logging each removal. `PermissionSyncOptions:RemoveRevoked=false` makes it add-only — use it for hosts that load only some modules. Custom roles are never touched.
- `SessionCleanupHostedService` — hourly expired-session purge; OCE handled by a preceding catch.

These are the model for background loops: stay alive, log with context, never swallow cancellation. See `api-conventions.md`.

## Tokens / sessions

Login `POST /api/v1/identity/token/issue` (header `X-FSH-App` enforces the operator/tenant app boundary). Refresh `POST /api/v1/identity/token/refresh` cross-checks subject. Session rows are written best-effort during login — failures log a warning and login still succeeds. Admin can't demote/deactivate the last admin or the root-tenant seed admin (guards in `UserRoleService`/`UserStatusService`).

## Tests

`Identity.Tests` is the largest unit suite. When asserting a forwarded `CancellationToken`, assert the specific token (see `testing.md`).

## Copy a role to the tenants (`POST /roles/{id}/copy-to-tenants`)

Root tenant only (`Permissions.Roles.CopyToTenants`, flagged `IsRoot`; the handler also refuses a request whose resolved tenant is not root). The seeder, `RolePermissionSyncer` and the DbMigrator touch only the built-in `Admin`/`Basic`, so a custom role of the root tenant exists nowhere else until it is copied. `CopyRoleToTenantsCommandHandler` walks the tenants (not root) and, in a scope per tenant built like `RolePermissionSyncHostedService` builds it, reuses `IRoleService`: a tenant with a role of that name is `Skipped`, otherwise the role is created and `UpdatePermissionsAsync` grants the permissions (root-only ones are stripped there, as for any non-root tenant). A role left without permissions by a failure is deleted again; one tenant failing is reported (`Failed`) and does not stop the rest. Built-in roles are refused (400).

Audit: the HTTP audit records the request, `AuditingSaveChangesInterceptor` the role and claims written in each tenant, and the handler adds an `Activity`/`Command` event `role.copy-to-tenant` per tenant plus `role.copy-to-tenants` in the root tenant, published through `IAuditPublisher` with the tenant and the operator named on the envelope and `AuditTag.Guaranteed` (the lane that waits). Bell: it queues `RoleCopiedToTenantsIntegrationEvent` through the outbox for the operator (`modules/notifications.md`). Audit and bell are best effort: a failure is logged and never undoes the copy.

---
name: add-permission
description: Add a new permission end-to-end — server constant + endpoint gate, and (admin app) mirror it into the permissions catalog + route guard. Use when a new endpoint needs authorization. See modules/identity.md + frontend/admin.md.
argument-hint: "[ModuleName] [Resource] [Action]"
---

# Add Permission

A permission spans server + both apps. The JWT carries only role names; both apps fetch the effective
permission list after sign-in and gate pages on it, while the server's 403 stays the real enforcement.

## Step 1 — Server constant (`Modules.{X}.Contracts/Authorization/{X}Permissions.cs`)

Add the constant to the resource group and ensure it's in the module's `All` collection. Convention:
`Permissions.{Resource}.{Action}`.

```csharp
public static class {X}Permissions
{
    public static class {Resources}
    {
        public const string View   = "Permissions.{Resources}.View";
        public const string Create = "Permissions.{Resources}.Create";   // ← new
    }
    public static IReadOnlyList<FshPermission> All { get; } = [ /* … include the new one … */ ];
}
```

The module already calls `PermissionConstants.Register({X}Permissions.All)` in `ConfigureServices`, so a new entry in `All` is picked up automatically.

## Step 2 — Gate the endpoint

```csharp
.RequirePermission({X}Permissions.{Resources}.Create);
```

⚠️ `RequiredPermissionAttribute` implements `IRequiredPermissionMetadata`. **Never let a second/duplicate of that interface exist** — it silently disables **all** `.RequirePermission()` gates app-wide. (See `.agents/rules/modules/identity.md`.)

## Step 3 — (admin only) mirror it

`clients/admin/src/lib/permissions.ts` — add the matching string to the frozen tree (only needed when a route guard or nav item references it):

```ts
export const {Module}Permissions = Object.freeze({
  {Resources}: { View: "Permissions.{Resources}.View", Create: "Permissions.{Resources}.Create" },
} as const);
```

The Role editor needs nothing: it renders the server catalog (`GET /api/v1/identity/permissions/catalog`), so the permission appears there once it is in the server's `All` collection.

## Step 4 — (admin only) gate the route

```tsx
{ path: "{resources}/new",
  element: <RouteGuard perms={[{Module}Permissions.{Resources}.Create]}><Create{Resource}Page /></RouteGuard> },
```

## Step 5 — (admin only) seed it in tests

So `RouteGuard` passes on first paint, add the new permission to the test seed set (`ADMIN_PERMS` in `clients/admin/tests/helpers/shell-mocks.ts`, used by `seedAuthedSession`).

## Dashboard

The app fetches the permission list from `GET /api/v1/identity/permissions` at hydration. If a **page** depends on the permission, add (or update) its entry in `ROUTE_GATES` (`src/lib/route-permissions.ts`) — the single source the sidebar (`nav-data.ts`), the command palette and the router (`guarded(ROUTE_GATES.x, …)` → Access denied) all read. A widget inside an ungated page gates its own query (`frontend/dashboard.md`). Playwright: grant it after `installShellMocks` with `grantPermissions(page, ["Permissions.X.Y"])`.

## Checklist

- [ ] Server constant added to `{X}Permissions` **and** its `All` collection
- [ ] Endpoint gated with `.RequirePermission(...)`; no duplicate `IRequiredPermissionMetadata`
- [ ] (admin) mirrored in `lib/permissions.ts` (only if a guard or nav item uses it; the role editor reads the server catalog)
- [ ] (admin) route wrapped in `<RouteGuard perms={[…]}>`; permission added to `ADMIN_PERMS` test seed
- [ ] (dashboard) page gate in `ROUTE_GATES` if a page depends on it; specs grant it via `grantPermissions`
- [ ] Build green; admin `test:e2e` green

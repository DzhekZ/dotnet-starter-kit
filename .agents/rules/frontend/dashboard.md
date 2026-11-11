# Frontend — dashboard app (`clients/dashboard`)

Tenant-facing application. Read `frontend/shared.md` first; this file is only the divergences.

- **Port** 5174 · dev proxy target `https://localhost:7030` (HTTPS, with `ws: true` for the SignalR hub) · localStorage prefix `fsh.dashboard.*` · login header `X-FSH-App: dashboard`.
- **Env** (`src/env.ts`): `{ apiBase, defaultTenant, demoMode }`.
- Dev-proxy is HTTPS on purpose: routing the bearer token through an HTTP→HTTPS 307 redirect stripped the `Authorization` header.

## No RHF/zod — hand-rolled forms

The dashboard does **not** depend on react-hook-form or zod. Use controlled inputs + local state. Don't add those deps to match admin.

## Permissions — fetched, not in the JWT

The JWT carries **only role names**. `auth-context.tsx` fetches the effective permission list from `GET /api/v1/identity/permissions` (`getMyPermissions()` in `src/api/identity.ts`), caches it in `tokenStore` under `fsh.dashboard.permissions`, and exposes a `permissionsHydrated` flag so gated UI doesn't flash while the fetch is in flight (re-fetched on login/impersonation swaps; `refreshPermissions()` for role changes). **Page gates live in one place:** `ROUTE_GATES` in `src/lib/route-permissions.ts` (`{ perm?, anyPerm? }`, mirroring each page's primary endpoint permission). Both consumers read it: the sidebar (`nav-data.ts`, spreads the gate into the item to hide it) and the router (`routes.tsx`, `guarded(ROUTE_GATES.x, <Page/>)` → `RouteGuard`), so a user who types the URL of a page hidden from them gets an in-shell **Access denied** (`ForbiddenView`) instead of a page whose every request 403s. `ProtectedRoute` stays auth-only; `RouteGuard` layers inside it, shows a spinner while `!permissionsHydrated`, and sits outside Suspense so a denied user never downloads the chunk. Live activity (`Permissions.Activity.View`) and System health (`Permissions.Health.View`, reads the gated `GET /api/v1/health` — the anonymous `/health/*` probes are for orchestrators) are gated like any other page. Pages open to every signed-in user (Overview, most Settings tabs) have no gate; widgets inside them that need a permission gate themselves (see below).

A query that mounts outside its own nav-gated page (topbar badges, overview cards) must set `enabled: permissionsHydrated && user.permissions.includes(<the endpoint's RequirePermission>)` and hide or replace its UI without it; otherwise every poll is a 403 the API logs as an error. `refetch()` ignores `enabled`, so gate manual refetches too. Playwright: `installShellMocks` grants `SHELL_PERMISSIONS` (the Basic-role grants these queries need); specs testing the missing-permission path re-mock `/identity/permissions`.

## Routing & realtime/SSE

- Every route element is wrapped in `withSuspense(node)` (per-route skeleton fallback); a permission-gated page uses `guarded(ROUTE_GATES.x, node)`, which wraps `withSuspense` in `RouteGuard`.
- `RealtimeProvider` **and** `SseProvider` are mounted inside `AppShell` (authenticated routes only), under a `CommandPaletteProvider` (cmdk).
- SignalR provider pre-wires ~11 chat/notification events.
- **SSE** (`src/sse/`, dashboard-only): connects only when the user holds `Permissions.Activity.View` (`ROUTE_GATES.activity`; status stays `idle` otherwise). Two-step token — `POST /api/v1/sse/token`, then `GET /api/v1/sse/stream?token=<guid>` consumed via fetch streaming (`parseSseStream` async generator; EventSource can't send auth headers). **Two split contexts:** `useSseStatus()` (stable, for status dots) vs `useSseEvents()` (mutates per event) to avoid cascading re-renders; `useSse()` is the composite.

## Impersonation

`token-store.ts` has `beginImpersonation` / `endImpersonationWithFreshTokens` / `restoreStashedActor` that stash the operator's tokens under `fsh.dashboard.impersonation.*`. `AuthProvider` exposes `beginImpersonation`/`stopImpersonation` and derives `ImpersonationInfo` from `act_sub` / `act_tenant` / `act_name` claims. Admin triggers the handoff one-way via its `dashboardUrl`.

## Performance

- `@tanstack/react-virtual` for long lists — use it for any large collection (chat history, big tables).
- `cmdk` powers the command palette.

## Theme

**Chroma-0 neutrals** (`--neutral-*: oklch(L 0 0)` — untinted; the warm-paper tint was deliberately removed). Rose default brand with **swappable accent themes** via `.accent-{rose,indigo,violet,sky,emerald,amber}` classes that override the `--brand-*` oklch stops; saffron secondary; Figtree font. Defined in `src/styles/globals.css`. Keep neutrals at chroma 0.

## Add-a-page deltas (on top of shared steps)

- Hand-roll forms (no RHF/zod).
- Wrap the route element in `withSuspense(<X/>)`. If the page's endpoint requires a permission, add a `ROUTE_GATES` entry and use `guarded(ROUTE_GATES.x, <X/>)` for the route **and** `...ROUTE_GATES.x` on its nav item.
- Playwright: a spec that opens a gated page must grant it after `installShellMocks` — `grantPermissions(page, ["Permissions.X.View"])` (shell defaults + extras).
- If it consumes pushes: `useRealtimeEvent("EventName", handler)` (register the name in `realtime-context.tsx`) or `useSseEvents()` for SSE.
- Use `react-virtual` for long lists; keep neutrals chroma 0.

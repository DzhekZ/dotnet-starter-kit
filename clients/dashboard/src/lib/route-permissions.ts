import { ALL_TRASH_PERMISSIONS } from "@/lib/trash-permissions";

/**
 * A permission gate for a page (route) or the nav entry that links to it.
 *
 * - `perm` — the single permission the user must hold.
 * - `anyPerm` — the user must hold *at least one* of these. Use for a page that
 *   fronts several independently-gated sub-views (e.g. Trash, whose tabs each
 *   require a different permission).
 *
 * Both are optional and combine with AND. An empty gate means "any signed-in
 * tenant user".
 */
export type PermissionGate = {
  perm?: string;
  anyPerm?: readonly string[];
};

/** True when `permissions` satisfies the gate. An empty gate always passes. */
export function isGateSatisfied(gate: PermissionGate, permissions: readonly string[]): boolean {
  if (gate.perm && !permissions.includes(gate.perm)) return false;
  if (gate.anyPerm && !gate.anyPerm.some((p) => permissions.includes(p))) return false;
  return true;
}

/** The permissions a gate is missing, for display on the access-denied view. */
export function missingPermissions(gate: PermissionGate, permissions: readonly string[]): string[] {
  const missing: string[] = [];
  if (gate.perm && !permissions.includes(gate.perm)) missing.push(gate.perm);
  if (gate.anyPerm && !gate.anyPerm.some((p) => permissions.includes(p))) {
    missing.push(...gate.anyPerm);
  }
  return missing;
}

/**
 * Per-page permission gates — the single source of truth shared by the sidebar
 * (`nav-data.ts`, hides the entry) and the router (`routes.tsx`, renders
 * Access Denied when the URL is opened directly).
 *
 * Each gate mirrors the permission the page's primary endpoint enforces
 * server-side. If an endpoint's permission changes, mirror it here.
 *
 * Pages that are not listed here (Overview, Settings profile/security/
 * appearance/notifications/api-keys) are open to every signed-in user: their
 * endpoints are auth-only or per-user, and any widget inside them that needs a
 * permission gates itself.
 */
export const ROUTE_GATES = {
  // Live activity streams SSE; POST /sse/token requires Activity.View.
  activity: { perm: "Permissions.Activity.View" },
  // System health reads GET /api/v1/health (the anonymous /health/* probes are
  // for orchestrators, not for the page).
  health: { perm: "Permissions.Health.View" },
  chat: { perm: "Permissions.Chat.Channels.View" },
  files: { perm: "Permissions.Files.Upload" },
  // Subscription, Wallet and Invoices are all billing views.
  billing: { perm: "Permissions.Billing.View" },
  products: { perm: "Permissions.Catalog.Products.View" },
  brands: { perm: "Permissions.Catalog.Brands.View" },
  categories: { perm: "Permissions.Catalog.Categories.View" },
  tickets: { perm: "Permissions.Tickets.View" },
  // Identity-management pages gate on a manage permission (not View): View
  // Users/Roles/Groups are IsBasic so every member holds them (the chat/user
  // picker relies on Users.View), but only managers should reach these admin
  // pages. Basic lacks the *.Update perms.
  users: { perm: "Permissions.Users.Update" },
  roles: { perm: "Permissions.Roles.Update" },
  groups: { perm: "Permissions.Groups.Update" },
  audits: { perm: "Permissions.AuditTrails.View" },
  sessions: { perm: "Permissions.Sessions.ViewAll" },
  // Trash fronts five tabs, each gated on a different resource's restore /
  // view-trash permission; reachable if the user can open any one of them.
  trash: { anyPerm: ALL_TRASH_PERMISSIONS },
  // Tenant-wide branding — same permission the /theme endpoints enforce.
  branding: { perm: "Permissions.Tenants.UpdateTheme" },
} as const satisfies Record<string, PermissionGate>;

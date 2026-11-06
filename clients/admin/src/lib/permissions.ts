/**
 * Permission strings + catalog mirrored from the server's *Permissions.cs
 * registries. Kept here so:
 *   1. Route guards stay typo-proof (`IdentityPermissions.Users.View`).
 *   2. The Role editor groups the permission catalog fetched from the server
 *      by resource (see the bottom of this file).
 *
 * Only permissions referenced by route guards or nav items need a constant
 * here; the editor picks up every other permission from the server itself.
 * Convention follows the server: `Permissions.{Resource}.{Action}`.
 */

import type { PermissionCatalogEntryDto } from "@/api/roles";

export const IdentityPermissions = Object.freeze({
  Users: {
    View: "Permissions.Users.View",
    Search: "Permissions.Users.Search",
    Create: "Permissions.Users.Create",
    Update: "Permissions.Users.Update",
    Delete: "Permissions.Users.Delete",
    Export: "Permissions.Users.Export",
    ManageRoles: "Permissions.Users.ManageRoles",
    Impersonate: "Permissions.Users.Impersonate",
  },
  UserRoles: {
    View: "Permissions.UserRoles.View",
    Update: "Permissions.UserRoles.Update",
  },
  Roles: {
    View: "Permissions.Roles.View",
    Create: "Permissions.Roles.Create",
    Update: "Permissions.Roles.Update",
    Delete: "Permissions.Roles.Delete",
    CopyToTenants: "Permissions.Roles.CopyToTenants",
  },
  RoleClaims: {
    View: "Permissions.RoleClaims.View",
    Update: "Permissions.RoleClaims.Update",
  },
  Sessions: {
    View: "Permissions.Sessions.View",
    Revoke: "Permissions.Sessions.Revoke",
    ViewAll: "Permissions.Sessions.ViewAll",
    RevokeAll: "Permissions.Sessions.RevokeAll",
  },
  Impersonation: {
    View: "Permissions.Impersonation.View",
    Revoke: "Permissions.Impersonation.Revoke",
  },
} as const);

export const MultitenancyPermissions = Object.freeze({
  Tenants: {
    View: "Permissions.Tenants.View",
    Create: "Permissions.Tenants.Create",
    Update: "Permissions.Tenants.Update",
    UpgradeSubscription: "Permissions.Tenants.UpgradeSubscription",
  },
} as const);

export const BillingPermissions = Object.freeze({
  View: "Permissions.Billing.View",
  Manage: "Permissions.Billing.Manage",
} as const);

export const AuditingPermissions = Object.freeze({
  AuditTrails: {
    View: "Permissions.AuditTrails.View",
    ViewCrossTenant: "Permissions.AuditTrails.ViewCrossTenant",
  },
} as const);

export const WebhooksPermissions = Object.freeze({
  Subscriptions: {
    View: "Permissions.Webhooks.View",
    Create: "Permissions.Webhooks.Create",
    Delete: "Permissions.Webhooks.Delete",
    Test: "Permissions.Webhooks.Test",
  },
} as const);

// ─── Catalog grouping (drives the Role editor) ──────────────────────────
// The catalog itself comes from GET /api/v1/identity/permissions/catalog
// (`getPermissionsCatalog` in `@/api/roles`), so there is no static copy to keep in sync.

export type PermissionGroup = {
  /** Stable catalog key. The English `category`/`blurb`/`description` below stay as the
   *  fallback, so an untranslated group still reads. */
  key: string;
  /** UI-facing category name. */
  category: string;
  /** Section blurb shown under the heading. */
  blurb: string;
    entries: PermissionCatalogEntryDto[];
};

/**
 * Buckets a flat catalog into groups keyed by resource, preserving the order
 * the API returned (registration order), so related actions stay together.
 */
export function groupPermissions(catalog: readonly PermissionCatalogEntryDto[]): PermissionGroup[] {
  const groups = new Map<string, PermissionGroup>();
  for (const entry of catalog) {
    const group = groups.get(entry.resource);
    if (group) group.entries.push(entry);
    else groups.set(entry.resource, { resource: entry.resource, entries: [entry] });
  }
  return Array.from(groups.values());
}

/** "AuditTrails" → "Audit Trails". */
export function formatResource(resource: string): string {
  return resource.replace(/([a-z0-9])([A-Z])/g, "$1 $2");
}

import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

// Hiding a nav entry isn't enough: a user who types the URL of a page they
// lack the permission for must get Access Denied, not the page. Gates live in
// src/lib/route-permissions.ts (ROUTE_GATES), shared with the sidebar.

// A deliberately narrow role (files + notifications + own sessions), like a
// tenant-created "Lite" role.
const LITE_PERMS = [
  "Permissions.Files.Upload",
  "Permissions.Files.DeleteOwn",
  "Permissions.Notifications.Inbox.View",
  "Permissions.Notifications.Inbox.MarkRead",
  "Permissions.Sessions.View",
  "Permissions.Sessions.Revoke",
];

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
  await mockJsonResponse(page, "**/api/v1/identity/permissions", LITE_PERMS);
});

const DENIED: { path: string; perm: string }[] = [
  { path: "/activity", perm: "Permissions.Activity.View" },
  { path: "/system/health", perm: "Permissions.Health.View" },
  { path: "/subscription", perm: "Permissions.Billing.View" },
  { path: "/wallet", perm: "Permissions.Billing.View" },
  { path: "/invoices", perm: "Permissions.Billing.View" },
  { path: "/invoices/inv-1", perm: "Permissions.Billing.View" },
  { path: "/chat", perm: "Permissions.Chat.Channels.View" },
  { path: "/catalog/products", perm: "Permissions.Catalog.Products.View" },
  { path: "/catalog/brands", perm: "Permissions.Catalog.Brands.View" },
  { path: "/catalog/categories", perm: "Permissions.Catalog.Categories.View" },
  { path: "/tickets", perm: "Permissions.Tickets.View" },
  { path: "/identity/users", perm: "Permissions.Users.Update" },
  { path: "/identity/roles", perm: "Permissions.Roles.Update" },
  { path: "/identity/groups", perm: "Permissions.Groups.Update" },
  { path: "/system/audits", perm: "Permissions.AuditTrails.View" },
  { path: "/system/sessions", perm: "Permissions.Sessions.ViewAll" },
  { path: "/system/trash", perm: "Permissions.Files.ViewTrash" },
  { path: "/settings/branding", perm: "Permissions.Tenants.UpdateTheme" },
];

for (const { path, perm } of DENIED) {
  test(`direct navigation to ${path} without permission shows access denied`, async ({ page }) => {
    await page.goto(path);

    const forbidden = page.getByTestId("forbidden-view");
    await expect(forbidden).toBeVisible();
    await expect(forbidden.getByRole("heading", { name: "Access denied" })).toBeVisible();
    await expect(forbidden.getByText(perm, { exact: true })).toBeVisible();
  });
}

test("a denied page never fires its API request", async ({ page }) => {
  let billingCalls = 0;
  await page.route("**/api/v1/billing/invoices**", (route) => {
    billingCalls += 1;
    return route.fulfill({ status: 403 });
  });

  await page.goto("/invoices");
  await expect(page.getByTestId("forbidden-view")).toBeVisible();
  expect(billingCalls).toBe(0);
});

test("the shell stays mounted so the user can navigate on", async ({ page }) => {
  await page.goto("/subscription");
  await expect(page.getByTestId("forbidden-view")).toBeVisible();

  // The sidebar still offers the pages the role can open.
  await page.getByRole("link", { name: "My files" }).first().click();
  await expect(page).toHaveURL(/\/files$/);
  await expect(page.getByTestId("forbidden-view")).toHaveCount(0);
});

test("without Activity.View the shell never requests an SSE token", async ({ page }) => {
  let tokenCalls = 0;
  await page.route("**/api/v1/sse/token", (route) => {
    tokenCalls += 1;
    return route.fulfill({ status: 403 });
  });

  await page.goto("/files");
  await expect(page.getByRole("link", { name: "My files" }).first()).toBeVisible();
  // Give the provider time to (not) start its connect loop.
  await page.waitForTimeout(500);
  expect(tokenCalls).toBe(0);
});

test("the sidebar drops the Operations and System sections without Activity/Health", async ({ page }) => {
  const sidebar = page.getByRole("complementary", { name: "Primary navigation" });

  // Lite role: every item in both sections is gated away, so the sections go too.
  await page.goto("/files");
  await expect(sidebar.getByRole("link", { name: "My files" })).toBeVisible();
  await expect(sidebar.getByRole("button", { name: "Operations" })).toHaveCount(0);
  await expect(sidebar.getByRole("button", { name: "System" })).toHaveCount(0);
});

test("Activity.View and Health.View bring the Operations and System sections back", async ({ page }) => {
  await mockJsonResponse(page, "**/api/v1/identity/permissions", [
    ...LITE_PERMS,
    "Permissions.Activity.View",
    "Permissions.Health.View",
  ]);
  const sidebar = page.getByRole("complementary", { name: "Primary navigation" });

  await page.goto("/files");
  await expect(sidebar.getByRole("button", { name: "Operations" })).toBeVisible();
  await expect(sidebar.getByRole("button", { name: "System" })).toBeVisible();
});

test("a granted permission opens the page", async ({ page }) => {
  await mockJsonResponse(page, "**/api/v1/identity/permissions", [
    ...LITE_PERMS,
    "Permissions.Billing.View",
  ]);

  await page.goto("/invoices");
  await expect(page.getByTestId("forbidden-view")).toHaveCount(0);
});

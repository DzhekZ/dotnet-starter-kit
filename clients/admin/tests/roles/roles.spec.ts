import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";

// listRoles hits the paged roles endpoint (`PagedResponse<RoleDto>`) and unwraps
// `.items`. System roles Admin/Basic sort first.
const ROLES = [
  { id: "role-manager", name: "Manager", description: "Manages a team" },
  { id: "role-admin", name: "Admin", description: "Full system access" },
  { id: "role-basic", name: "Basic", description: "Default authenticated role" },
];

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("roles list", () => {
  test("renders the Roles heading and a role row from the mock data", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/roles", paged(ROLES));

    await page.goto("/roles");

    const main = page.getByRole("main");
    await expect(main.getByRole("heading", { name: "Roles", exact: true })).toBeVisible({
      timeout: 10_000,
    });
    // Each role renders in both a mobile card (hidden at desktop width) and a
    // desktop row button whose accessible name starts with the role name
    // ("Manager Manages a team —"). The mobile button is labelled "Open role …",
    // so a name anchored to the start uniquely targets the visible desktop row.
    await expect(main.getByRole("button", { name: /^Manager\b/ })).toBeVisible();
    await expect(main.getByRole("button", { name: /^Admin\b/ })).toBeVisible();
    await expect(main.getByRole("button", { name: /^Basic\b/ })).toBeVisible();
    // System roles (Admin/Basic) get a "System" badge — scope to the visible
    // desktop Admin row so we don't match the hidden mobile-card duplicate.
    await expect(
      main.getByRole("button", { name: /^Admin\b/ }).getByText("System", { exact: true }),
    ).toBeVisible();
  });

  test("shows the empty state when no roles are defined", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/roles", paged([]));

    await page.goto("/roles");

    const main = page.getByRole("main");
    await expect(main.getByText("No roles defined yet.", { exact: true })).toBeVisible({
      timeout: 10_000,
    });
    await expect(main.getByText("Create your first role to start bundling permissions.")).toBeVisible();
  });
});

test.describe("roles create form", () => {
  // Creation is now a Radix dialog opened from the "New role" button on the
  // /roles list page — the old /roles/new route just redirects to /roles.
  // Mock a populated list so the empty-state's own "New role" button doesn't
  // collide with the header trigger.
  test("renders the name + description fields and the create action", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/roles", paged(ROLES), { method: "GET" });

    await page.goto("/roles");

    const main = page.getByRole("main");
    await main.getByRole("button", { name: "New role" }).click();

    const dialog = page.getByRole("dialog");
    await expect(
      dialog.getByRole("heading", { name: "New role", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(dialog.getByLabel(/^Name/)).toBeVisible();
    await expect(dialog.getByLabel(/^Description/)).toBeVisible();

    await expect(
      dialog.getByRole("button", { name: "Create role", exact: true }),
    ).toBeVisible();
  });

  test("filling the form and submitting POSTs to /identity/roles with an empty id", async ({ page }) => {
    // GET roles list (registered first so the POST handler's fallback reaches it).
    await mockJsonResponse(page, "**/api/v1/identity/roles", paged(ROLES), { method: "GET" });
    await page.route("**/api/v1/identity/roles", async (route) => {
      if (route.request().method() !== "POST") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id: "role-support", name: "Support agent", description: "Inbound support" }),
      });
    });
    // Success navigates to the role detail page — mock its permissions load.
    await mockJsonResponse(page, "**/api/v1/identity/role-support/permissions", {
      id: "role-support",
      name: "Support agent",
      description: "Inbound support",
      permissions: [],
    });

    await page.goto("/roles");
    const main = page.getByRole("main");
    await main.getByRole("button", { name: "New role" }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByLabel(/^Name/)).toBeVisible({ timeout: 10_000 });

    await dialog.getByLabel(/^Name/).fill("Support agent");
    await dialog.getByLabel(/^Description/).fill("Inbound support");

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/identity/roles") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await dialog.getByRole("button", { name: "Create role", exact: true }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body).toMatchObject({
      id: "",
      name: "Support agent",
      description: "Inbound support",
    });
  });

  test("client-side validation blocks submit when the name is too short", async ({ page }) => {
    let posted = false;
    await page.route("**/api/v1/identity/roles", async (route) => {
      if (route.request().method() === "POST") {
        posted = true;
        await route.fulfill({
          status: 200,
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ id: "x", name: "x" }),
        });
        return;
      }
      // GET roles list — keep the list populated so only the header trigger exists.
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged(ROLES)),
      });
    });

    await page.goto("/roles");
    const main = page.getByRole("main");
    await main.getByRole("button", { name: "New role" }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByLabel(/^Name/)).toBeVisible({ timeout: 10_000 });

    await dialog.getByLabel(/^Name/).fill("A");
    await dialog.getByRole("button", { name: "Create role", exact: true }).click();

    await expect(dialog.getByText(/At least 2 characters\./i)).toBeVisible();
    expect(posted).toBe(false);
  });
});

// GET /identity/permissions/catalog — the editor renders exactly what the server returns.
// Norms stands in for a module the admin app has no constants for.
const CATALOG = [
  { name: "Permissions.Users.View", description: "View users", resource: "Users", action: "View", isBasic: true, isRoot: false },
  { name: "Permissions.Users.Create", description: "Create users", resource: "Users", action: "Create", isBasic: false, isRoot: false },
  { name: "Permissions.Roles.View", description: "View roles", resource: "Roles", action: "View", isBasic: true, isRoot: false },
  { name: "Permissions.Sessions.View", description: "View my sessions", resource: "Sessions", action: "View", isBasic: true, isRoot: false },
  { name: "Permissions.Norms.View", description: "View norms", resource: "Norms", action: "View", isBasic: true, isRoot: false },
  { name: "Permissions.Norms.Upload", description: "Upload norms", resource: "Norms", action: "Upload", isBasic: false, isRoot: false },
  { name: "Permissions.AuditTrails.ViewCrossTenant", description: "View audit trails across tenants", resource: "AuditTrails", action: "ViewCrossTenant", isBasic: false, isRoot: true },
];

test.describe("role detail permission matrix", () => {
  const ROLE = {
    id: "role-manager",
    name: "Manager",
    description: "Manages a team",
    permissions: ["Permissions.Users.View", "Permissions.Roles.View"],
  };

  test.beforeEach(async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions/catalog", CATALOG);
  });

  test("loads the role and renders the profile + permission catalog groups", async ({ page }) => {
    // NOTE: path is /identity/{id}/permissions — NO /roles/ segment.
    await mockJsonResponse(page, `**/api/v1/identity/${ROLE.id}/permissions`, ROLE);

    await page.goto(`/roles/${ROLE.id}`);

    const main = page.getByRole("main");
    // PageHeader title shows the role name.
    await expect(main.getByRole("heading", { name: "Manager", exact: true })).toBeVisible({
      timeout: 10_000,
    });

    // Hydrated profile name field.
    await expect(main.getByLabel(/^Name/)).toHaveValue("Manager");

    // Section heading + catalog groups (rendered client-side from PERMISSION_CATALOG).
    // SettingsSection renders the title as a plain <h2>Permissions</h2>.
    await expect(
      main.getByRole("heading", { name: "Permissions", exact: true }),
    ).toBeVisible();
    await expect(main.getByRole("heading", { name: "Users", exact: true })).toBeVisible();
    await expect(main.getByRole("heading", { name: "Roles", exact: true })).toBeVisible();
    await expect(main.getByRole("heading", { name: "Sessions", exact: true })).toBeVisible();

    // A specific permission entry from the catalog.
    await expect(main.getByText("Permissions.Users.View", { exact: true })).toBeVisible();
  });

  test("toggling a permission and saving PUTs to /identity/{id}/permissions", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/identity/${ROLE.id}/permissions`, ROLE);

    await page.goto(`/roles/${ROLE.id}`);
    const main = page.getByRole("main");
    await expect(main.getByRole("heading", { name: "Manager", exact: true })).toBeVisible({
      timeout: 10_000,
    });

    // Toggle a not-yet-granted permission ("Create users").
    await main.getByText("Create users", { exact: true }).click();

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().endsWith(`/api/v1/identity/${ROLE.id}/permissions`) && r.method() === "PUT",
      { timeout: 5_000 },
    );
    await main.getByRole("button", { name: /save permissions/i }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.roleId).toBe(ROLE.id);
    expect(body.permissions).toContain("Permissions.Users.Create");
    // Pre-existing grants are preserved.
    expect(body.permissions).toContain("Permissions.Users.View");
  });

  test("renders a card per catalog resource, including modules unknown to the admin app", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/identity/${ROLE.id}/permissions`, ROLE);

    await page.goto(`/roles/${ROLE.id}`);
    const main = page.getByRole("main");

    await expect(main.getByRole("heading", { name: "Norms", exact: true })).toBeVisible({
      timeout: 10_000,
    });
    await expect(main.getByText("Permissions.Norms.Upload", { exact: true })).toBeVisible();
    // PascalCase resource names are split into words.
    await expect(main.getByRole("heading", { name: "Audit Trails", exact: true })).toBeVisible();
  });

  test("the footer counts only granted permissions that exist in the catalog", async ({ page }) => {
    // Two grants the catalog does not know (renamed or root-only elsewhere) must not
    // inflate the figure: 2 of 7, not 4 of 7.
    await mockJsonResponse(page, `**/api/v1/identity/${ROLE.id}/permissions`, {
      ...ROLE,
      permissions: [...ROLE.permissions, "Permissions.Legacy.Gone", "Permissions.Other.Gone"],
    });

    await page.goto(`/roles/${ROLE.id}`);
    const main = page.getByRole("main");

    await expect(main.getByText("All changes saved · 02 of 07 granted")).toBeVisible({
      timeout: 10_000,
    });
  });

  test("selecting a whole module card and saving PUTs its permissions", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/identity/${ROLE.id}/permissions`, ROLE);

    await page.goto(`/roles/${ROLE.id}`);
    const main = page.getByRole("main");
    const card = main.locator("div.overflow-hidden", {
      has: page.getByRole("heading", { name: "Norms", exact: true }),
    });

    await card.getByRole("button", { name: "Select all" }).click();
    await expect(main.getByText(/Unsaved changes · 04 of 07 granted/)).toBeVisible();

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().endsWith(`/api/v1/identity/${ROLE.id}/permissions`) && r.method() === "PUT",
      { timeout: 5_000 },
    );
    await main.getByRole("button", { name: /save permissions/i }).click();
    const body = JSON.parse((await reqPromise).postData() ?? "{}");

    expect(body.permissions).toEqual(
      expect.arrayContaining(["Permissions.Norms.View", "Permissions.Norms.Upload", "Permissions.Users.View"]),
    );
  });

  test("only <main> scrolls: the document itself never gets a second scrollbar", async ({ page }) => {
    // A short window forces <main> to scroll. The permission checkboxes are visually
    // hidden (sr-only, absolutely positioned); when they were not anchored to their card
    // they extended the document's own scroll height as well.
    await page.setViewportSize({ width: 1333, height: 449 });
    await mockJsonResponse(page, `**/api/v1/identity/${ROLE.id}/permissions`, ROLE);

    await page.goto(`/roles/${ROLE.id}`);
    await expect(page.getByRole("main").getByText("Permissions.Norms.Upload", { exact: true })).toBeVisible({
      timeout: 10_000,
    });

    const doc = await page.evaluate(() => ({
      scrollHeight: document.documentElement.scrollHeight,
      clientHeight: document.documentElement.clientHeight,
    }));
    expect(doc.scrollHeight).toBe(doc.clientHeight);
  });
});

test.describe("role detail: Copy to Tenants", () => {
  const ROLE = {
    id: "role-manager",
    name: "Manager",
    description: "Manages a team",
    permissions: ["Permissions.Users.View"],
  };

  const copyButton = (page: import("@playwright/test").Page) =>
    page.getByRole("main").getByRole("button", { name: "Copy to Tenants", exact: true });

  test.beforeEach(async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions/catalog", CATALOG);
    await mockJsonResponse(page, `**/api/v1/identity/${ROLE.id}/permissions`, ROLE);
  });

  test("confirming POSTs to the copy endpoint and reports the outcome", async ({ page }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/identity/roles/${ROLE.id}/copy-to-tenants`,
      {
        results: [
          { tenantId: "acme", status: "Created" },
          { tenantId: "beta", status: "Skipped" },
        ],
      },
      { method: "POST" },
    );

    await page.goto(`/roles/${ROLE.id}`);
    await expect(copyButton(page)).toBeVisible({ timeout: 10_000 });
    await copyButton(page).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Copy role to tenants?" })).toBeVisible();

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith(`/api/v1/identity/roles/${ROLE.id}/copy-to-tenants`) && r.method() === "POST",
      { timeout: 5_000 },
    );
    await dialog.getByRole("button", { name: "Copy to Tenants", exact: true }).click();
    await reqPromise;

    await expect(page.getByText("Role copied to tenants")).toBeVisible();
    await expect(page.getByText("1 created, 1 already had it")).toBeVisible();
  });

  test("lists the tenants the copy failed for", async ({ page }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/identity/roles/${ROLE.id}/copy-to-tenants`,
      { results: [{ tenantId: "acme", status: "Failed", error: "permission store is down" }] },
      { method: "POST" },
    );

    await page.goto(`/roles/${ROLE.id}`);
    await copyButton(page).click();
    await page.getByRole("dialog").getByRole("button", { name: "Copy to Tenants", exact: true }).click();

    await expect(page.getByText("Copy failed for 1 tenant")).toBeVisible();
    await expect(page.getByText(/acme: permission store is down/)).toBeVisible();
  });

  test("is hidden for a built-in role", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/role-admin/permissions", {
      id: "role-admin",
      name: "Admin",
      permissions: [],
    });

    await page.goto("/roles/role-admin");
    await expect(page.getByRole("main").getByLabel(/^Name/)).toHaveValue("Admin", { timeout: 10_000 });

    await expect(copyButton(page)).toHaveCount(0);
  });

  test("is hidden outside the root tenant", async ({ page }) => {
    await seedAuthedSession(page, { ...TEST_USER, tenant: "acme", permissions: [...ADMIN_PERMS] });

    await page.goto(`/roles/${ROLE.id}`);
    await expect(page.getByRole("main").getByLabel(/^Name/)).toHaveValue("Manager", { timeout: 10_000 });

    await expect(copyButton(page)).toHaveCount(0);
  });

  test("is hidden without the CopyToTenants permission", async ({ page }) => {
    const perms = ADMIN_PERMS.filter((p) => p !== "Permissions.Roles.CopyToTenants");
    await seedAuthedSession(page, { ...TEST_USER, permissions: perms });
    await mockJsonResponse(page, "**/api/v1/identity/permissions", perms);

    await page.goto(`/roles/${ROLE.id}`);
    await expect(page.getByRole("main").getByLabel(/^Name/)).toHaveValue("Manager", { timeout: 10_000 });

    await expect(copyButton(page)).toHaveCount(0);
  });

  test("is disabled while the profile has unsaved edits", async ({ page }) => {
    await page.goto(`/roles/${ROLE.id}`);
    const name = page.getByRole("main").getByLabel(/^Name/);
    await expect(name).toHaveValue("Manager", { timeout: 10_000 });

    await name.fill("Manager 2");

    await expect(copyButton(page)).toBeDisabled();
  });
});

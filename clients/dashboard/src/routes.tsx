import { lazy, Suspense, type ComponentType } from "react";
import { createBrowserRouter, Navigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { AppShell } from "@/components/layout/app-shell";
import { ProtectedRoute } from "@/auth/protected-route";
import { RouteGuard } from "@/auth/route-guard";
import { RouteError } from "@/components/route-error";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/cn";
import { loadNamespaces, type LazyNamespace } from "@/i18n";
import { ROUTE_GATES, type PermissionGate } from "@/lib/route-permissions";

// ─────────────────────────────────────────────────────────────────────────
// Lazy route boundaries
//
// Each route is split into its own bundle so first paint after login only
// loads the chunk for the landing page (Overview). Catalog, Settings,
// Login, Activity, etc. fetch on demand.
//
// `lazyNamed` wraps a dynamic import that exports a *named* component
// (we don't use default exports anywhere in this codebase). Returns a
// shape compatible with React.lazy.
//
// `namespaces` lists the route-only translation catalogs the page (and the
// components it imports) use. They load in parallel with the page chunk, so
// the page never renders before its text is there. The shell's namespaces
// (common, auth, notifications) are always loaded and are not listed.
// ─────────────────────────────────────────────────────────────────────────

function lazyNamed<T extends Record<string, unknown>, K extends keyof T>(
  importer: () => Promise<T>,
  name: K,
  namespaces: readonly LazyNamespace[] = [],
) {
  return lazy(async () => {
    const [mod] = await Promise.all([importer(), loadNamespaces(namespaces)]);
    return { default: mod[name] as ComponentType<unknown> };
  });
}

const LoginPage = lazyNamed(() => import("@/pages/login"), "LoginPage");
const ForgotPasswordPage = lazyNamed(
  () => import("@/pages/auth/forgot-password"),
  "ForgotPasswordPage",
);
const ResetPasswordPage = lazyNamed(
  () => import("@/pages/auth/reset-password"),
  "ResetPasswordPage",
);
const ConfirmEmailPage = lazyNamed(
  () => import("@/pages/auth/confirm-email"),
  "ConfirmEmailPage",
);
const OverviewPage = lazyNamed(() => import("@/pages/overview"), "OverviewPage", ["overview"]);
const ActivityPage = lazyNamed(() => import("@/pages/activity"), "ActivityPage", ["activity", "audits"]);
const InvoicesPage = lazyNamed(() => import("@/pages/invoices"), "InvoicesPage", ["subscription"]);
const InvoiceDetailPage = lazyNamed(() => import("@/pages/invoice-detail"), "InvoiceDetailPage", ["subscription"]);
const SubscriptionPage = lazyNamed(() => import("@/pages/subscription"), "SubscriptionPage", ["subscription"]);
const WalletPage = lazyNamed(() => import("@/pages/wallet"), "WalletPage", ["subscription"]);
const BrandsPage = lazyNamed(() => import("@/pages/catalog/brands"), "BrandsPage", ["catalog"]);
const CategoriesPage = lazyNamed(() => import("@/pages/catalog/categories"), "CategoriesPage", ["catalog"]);
const ProductsPage = lazyNamed(() => import("@/pages/catalog/products"), "ProductsPage", ["catalog"]);
const ProductDetailPage = lazyNamed(() => import("@/pages/catalog/product-detail"), "ProductDetailPage", ["catalog", "files"]);
const NotFoundPage = lazyNamed(() => import("@/pages/not-found"), "NotFoundPage");
const TenantDeactivatedPage = lazyNamed(
  () => import("@/pages/tenant-deactivated"),
  "TenantDeactivatedPage",
);
const ImpersonationEndedPage = lazyNamed(
  () => import("@/pages/impersonation-ended"),
  "ImpersonationEndedPage",
);
const SettingsLayout = lazyNamed(() => import("@/pages/settings/settings-layout"), "SettingsLayout", ["settings"]);
const ProfileSettings = lazyNamed(() => import("@/pages/settings/profile"), "ProfileSettings", ["settings", "files"]);
const SecuritySettings = lazyNamed(() => import("@/pages/settings/security"), "SecuritySettings", ["settings"]);
const BrandingSettings = lazyNamed(() => import("@/pages/settings/branding"), "BrandingSettings", ["settings"]);
const AppearanceSettings = lazyNamed(() => import("@/pages/settings/appearance"), "AppearanceSettings", ["settings"]);
const NotificationsSettings = lazyNamed(() => import("@/pages/settings/notifications"), "NotificationsSettings", ["settings"]);
const ApiKeysSettings = lazyNamed(() => import("@/pages/settings/api-keys"), "ApiKeysSettings", ["settings"]);
const HealthPage = lazyNamed(() => import("@/pages/health"), "HealthPage", ["health"]);
const AuditsPage = lazyNamed(() => import("@/pages/audits"), "AuditsPage", ["audits"]);
const TicketsPage = lazyNamed(() => import("@/pages/tickets/tickets"), "TicketsPage", ["tickets"]);
const TicketDetailPage = lazyNamed(() => import("@/pages/tickets/ticket-detail"), "TicketDetailPage", ["tickets"]);
const TrashPage = lazyNamed(() => import("@/pages/system/trash"), "TrashPage", ["system"]);
const SessionsPage = lazyNamed(() => import("@/pages/system/sessions"), "SessionsPage", ["system"]);
const UsersPage = lazyNamed(() => import("@/pages/identity/users"), "UsersPage", ["identity"]);
const UserDetailPage = lazyNamed(() => import("@/pages/identity/user-detail"), "UserDetailPage", ["identity"]);
const RolesPage = lazyNamed(() => import("@/pages/identity/roles"), "RolesPage", ["identity"]);
const RoleDetailPage = lazyNamed(() => import("@/pages/identity/role-detail"), "RoleDetailPage", ["identity"]);
const GroupsPage = lazyNamed(() => import("@/pages/identity/groups"), "GroupsPage", ["identity"]);
const GroupDetailPage = lazyNamed(() => import("@/pages/identity/group-detail"), "GroupDetailPage", ["identity"]);
const MyFilesPage = lazyNamed(() => import("@/pages/files/my-files"), "MyFilesPage", ["files"]);
const ChatPage = lazyNamed(() => import("@/pages/chat/chat-page"), "ChatPage", ["chat"]);

/**
 * RouteFallback — what shows while a lazy chunk is downloading. Mirrors
 * the resolved page's shape: a thin header line, an atmospheric hero
 * placeholder, and three card placeholders. Uses the modernized
 * `.skeleton` shimmer so it feels like part of the same surface family.
 */
function RouteFallback() {
  const { t } = useTranslation("common");
  return (
    <div className={cn("space-y-6 fsh-enter")} role="status" aria-busy="true">
      <span className="sr-only">{t("list.loading")}</span>
      <div className="space-y-2">
        <Skeleton className="h-3 w-24" />
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-3 w-96 max-w-full" />
      </div>
      <Skeleton className="h-32 w-full rounded-[20px]" />
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        <Skeleton className="h-24 rounded-xl" />
        <Skeleton className="h-24 rounded-xl" />
        <Skeleton className="h-24 rounded-xl" />
      </div>
    </div>
  );
}

/**
 * Wrap a lazy route element in Suspense so each route boundary has its
 * own loading state. Returns a fresh Suspense per call — important so
 * that two routes sharing the same fallback don't share the same
 * suspended chunk's resolution timing.
 */
function withSuspense(node: React.ReactNode) {
  return <Suspense fallback={<RouteFallback />}>{node}</Suspense>;
}

/**
 * Wrap a permission-gated route: the guard renders Access Denied (inside the
 * AppShell) when the user lacks the gate, so typing the URL of a page hidden
 * from the sidebar no longer opens it. The guard sits outside Suspense so a
 * denied user never downloads the page chunk. Gates live in ROUTE_GATES,
 * shared with nav-data.ts.
 */
function guarded(gate: PermissionGate, node: React.ReactNode) {
  return <RouteGuard gate={gate}>{withSuspense(node)}</RouteGuard>;
}

export const router = createBrowserRouter([
  {
    path: "/login",
    element: withSuspense(<LoginPage />),
    errorElement: <RouteError />,
  },
  {
    path: "/forgot-password",
    element: withSuspense(<ForgotPasswordPage />),
    errorElement: <RouteError />,
  },
  {
    path: "/reset-password",
    element: withSuspense(<ResetPasswordPage />),
    errorElement: <RouteError />,
  },
  {
    path: "/confirm-email",
    element: withSuspense(<ConfirmEmailPage />),
    errorElement: <RouteError />,
  },
  {
    // Terminal state when the signed-in user's tenant is deactivated mid-session.
    // Top-level (outside ProtectedRoute/AppShell) — the token is still valid but
    // every request 403s, so there is no shell to render. query-client.ts routes
    // here on detecting the deactivated-tenant 403.
    path: "/tenant-deactivated",
    element: withSuspense(<TenantDeactivatedPage />),
    errorElement: <RouteError />,
  },
  {
    // Terminal state when an operator's impersonation grant is revoked (or its
    // short-lived token expires) mid-session. Top-level (outside
    // ProtectedRoute/AppShell) — the token still decodes but every request
    // 401s, so there is no shell to render. query-client.ts routes here on
    // detecting the impersonation-revoked 401.
    path: "/impersonation-ended",
    element: withSuspense(<ImpersonationEndedPage />),
    errorElement: <RouteError />,
  },
  {
    element: <ProtectedRoute />,
    errorElement: <RouteError />,
    children: [
      {
        element: <AppShell />,
        errorElement: <RouteError />,
        children: [
          { index: true, element: withSuspense(<OverviewPage />) },
          { path: "activity", element: guarded(ROUTE_GATES.activity, <ActivityPage />) },
          { path: "subscription", element: guarded(ROUTE_GATES.billing, <SubscriptionPage />) },
          { path: "wallet", element: guarded(ROUTE_GATES.billing, <WalletPage />) },
          { path: "invoices", element: guarded(ROUTE_GATES.billing, <InvoicesPage />) },
          { path: "invoices/:id", element: guarded(ROUTE_GATES.billing, <InvoiceDetailPage />) },
          { path: "system/health", element: guarded(ROUTE_GATES.health, <HealthPage />) },
          { path: "system/audits", element: guarded(ROUTE_GATES.audits, <AuditsPage />) },
          { path: "system/trash", element: guarded(ROUTE_GATES.trash, <TrashPage />) },
          { path: "system/sessions", element: guarded(ROUTE_GATES.sessions, <SessionsPage />) },
          { path: "files", element: guarded(ROUTE_GATES.files, <MyFilesPage />) },
          { path: "chat", element: guarded(ROUTE_GATES.chat, <ChatPage />) },
          { path: "chat/:channelId", element: guarded(ROUTE_GATES.chat, <ChatPage />) },
          { path: "tickets", element: guarded(ROUTE_GATES.tickets, <TicketsPage />) },
          { path: "tickets/:ticketId", element: guarded(ROUTE_GATES.tickets, <TicketDetailPage />) },
          { path: "identity", element: <Navigate to="/identity/users" replace /> },
          { path: "identity/users", element: guarded(ROUTE_GATES.users, <UsersPage />) },
          { path: "identity/users/:userId", element: guarded(ROUTE_GATES.users, <UserDetailPage />) },
          { path: "identity/roles", element: guarded(ROUTE_GATES.roles, <RolesPage />) },
          { path: "identity/roles/:roleId", element: guarded(ROUTE_GATES.roles, <RoleDetailPage />) },
          { path: "identity/groups", element: guarded(ROUTE_GATES.groups, <GroupsPage />) },
          { path: "identity/groups/:groupId", element: guarded(ROUTE_GATES.groups, <GroupDetailPage />) },
          { path: "catalog", element: <Navigate to="/catalog/brands" replace /> },
          { path: "catalog/brands", element: guarded(ROUTE_GATES.brands, <BrandsPage />) },
          { path: "catalog/categories", element: guarded(ROUTE_GATES.categories, <CategoriesPage />) },
          { path: "catalog/products", element: guarded(ROUTE_GATES.products, <ProductsPage />) },
          {
            path: "catalog/products/:productId",
            element: guarded(ROUTE_GATES.products, <ProductDetailPage />),
          },
          {
            path: "settings",
            element: withSuspense(<SettingsLayout />),
            children: [
              { index: true, element: <Navigate to="profile" replace /> },
              { path: "profile", element: withSuspense(<ProfileSettings />) },
              { path: "security", element: withSuspense(<SecuritySettings />) },
              { path: "appearance", element: withSuspense(<AppearanceSettings />) },
              { path: "branding", element: guarded(ROUTE_GATES.branding, <BrandingSettings />) },
              { path: "notifications", element: withSuspense(<NotificationsSettings />) },
              { path: "api-keys", element: withSuspense(<ApiKeysSettings />) },
            ],
          },
        ],
      },
    ],
  },
  { path: "*", element: withSuspense(<NotFoundPage />) },
]);

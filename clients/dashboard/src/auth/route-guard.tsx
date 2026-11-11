import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { useAuth } from "@/auth/use-auth";
import { ForbiddenView } from "@/components/forbidden-view";
import { missingPermissions, type PermissionGate } from "@/lib/route-permissions";

type RouteGuardProps = {
  /** The page's permission gate — use an entry of `ROUTE_GATES`. */
  gate: PermissionGate;
  children: ReactNode;
};

/**
 * RouteGuard — per-route permission wrapper, layered inside ProtectedRoute
 * (which only answers "are you signed in?"). Hiding a nav entry isn't enough:
 * a user can still type the URL, so the route itself renders an access-denied
 * view instead of a page whose every request would 403.
 *
 * The JWT carries only role names; permissions are fetched into AuthContext
 * after sign-in. While that fetch is in flight (permissionsHydrated=false) we
 * render a quiet loading state rather than flash "access denied".
 */
export function RouteGuard({ gate, children }: RouteGuardProps) {
  const { user, permissionsHydrated } = useAuth();
  const { t } = useTranslation("common");

  if (!permissionsHydrated) {
    return (
      <div
        className="flex min-h-[60vh] items-center justify-center text-sm text-muted-foreground"
        role="status"
        aria-busy="true"
      >
        <span className="sr-only">{t("forbidden.resolving")}</span>
        <span
          className="size-5 animate-spin rounded-full border-2 border-current border-t-transparent"
          aria-hidden
        />
      </div>
    );
  }

  const missing = missingPermissions(gate, user?.permissions ?? []);
  if (missing.length > 0) {
    return <ForbiddenView missing={missing} />;
  }

  return <>{children}</>;
}

import { Link, useLocation, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ArrowLeft, Home, ShieldX } from "lucide-react";
import { Button } from "@/components/ui/button";

/**
 * ForbiddenView — "access denied" state for a page the signed-in user lacks
 * the permission for. Rendered by RouteGuard inside the AppShell (the sidebar
 * and topbar stay), so the user can navigate on to a page they can open.
 *
 * Same calm vocabulary as NotFoundPage: a muted icon tile, a display
 * headline, a soft body line and the primary affordances.
 */
export function ForbiddenView({ missing }: { missing: readonly string[] }) {
  const { t } = useTranslation("common");
  const navigate = useNavigate();
  const location = useLocation();

  return (
    <div
      className="flex min-h-[60vh] items-center justify-center px-5 py-8 sm:py-12"
      data-testid="forbidden-view"
    >
      <div className="flex w-full max-w-[460px] flex-col items-center text-center fsh-enter fsh-enter-1">
        <div className="mb-5 grid size-14 place-items-center rounded-2xl bg-[var(--color-muted)]">
          <ShieldX className="size-6 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />
        </div>

        <h1 className="mb-2 font-display text-display-stat font-semibold tracking-tight text-[var(--color-foreground)]">
          {t("forbidden.title")}
        </h1>
        <p className="mb-2 max-w-[380px] text-[14px] leading-relaxed text-[var(--color-muted-foreground)]">
          {t("forbidden.body")}
        </p>

        <p
          className="mb-2 inline-flex max-w-full items-center gap-1.5 text-[12px] text-[var(--color-muted-foreground)]"
          title={location.pathname}
        >
          <span className="text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
            {t("forbidden.requested")}
          </span>
          <span className="truncate text-[var(--color-foreground)]">{location.pathname}</span>
        </p>

        {missing.length > 0 && (
          <div className="mb-7 flex max-w-full flex-col items-center gap-1 text-[12px] text-[var(--color-muted-foreground)]">
            <span className="text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
              {t("forbidden.required", { count: missing.length })}
            </span>
            <ul className="flex max-w-full flex-wrap justify-center gap-1.5">
              {missing.map((perm) => (
                <li key={perm}>
                  <code className="rounded-md bg-[var(--color-muted)] px-1.5 py-0.5 text-[11px] text-[var(--color-foreground)]">
                    {perm}
                  </code>
                </li>
              ))}
            </ul>
          </div>
        )}

        <div className="flex w-full flex-col items-center gap-2 sm:flex-row sm:justify-center">
          <Button asChild className="group h-11 px-5 text-[14px] font-semibold">
            <Link to="/">
              <Home className="size-4" />
              <span>{t("forbidden.backHome")}</span>
            </Link>
          </Button>
        </div>

        <button
          type="button"
          onClick={() => navigate(-1)}
          className="mt-5 inline-flex items-center gap-1.5 text-[12px] text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
        >
          <ArrowLeft className="size-3.5" />
          <span>{t("forbidden.goBack")}</span>
        </button>
      </div>
    </div>
  );
}

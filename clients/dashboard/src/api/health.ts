import { apiFetch } from "@/lib/api-client";

export type HealthStatus = "Healthy" | "Degraded" | "Unhealthy";

export type HealthEntry = {
  name: string;
  status: HealthStatus;
  description?: string | null;
  durationMs: number;
  details?: Record<string, unknown> | null;
};

export type HealthResult = {
  status: HealthStatus;
  results: HealthEntry[];
};

export type HealthSnapshot = HealthResult & {
  /** Wall-clock time the response landed in the browser. */
  fetchedAt: string;
  /** Round-trip from request start to response parse, in ms. */
  roundTripMs: number;
  /**
   * The code the readiness probe (`/health/ready`) answers for this report:
   * 200 when Healthy, 503 otherwise. The report endpoint itself always
   * returns 200, so this is derived from the overall status.
   */
  httpStatus: number;
};

/**
 * Fetch the detailed health report from `GET /api/v1/health` — the
 * permission-gated endpoint (Permissions.Health.View) behind the System health
 * page. Goes through apiFetch (auth, tenant, refresh). The anonymous
 * `/health/live` and `/health/ready` probes are for orchestrators.
 */
export async function getReadiness(signal?: AbortSignal): Promise<HealthSnapshot> {
  const t0 = performance.now();
  const body = await apiFetch<HealthResult>("/api/v1/health", { signal });
  const roundTripMs = performance.now() - t0;

  return {
    ...body,
    fetchedAt: new Date().toISOString(),
    roundTripMs,
    httpStatus: body.status === "Healthy" ? 200 : 503,
  };
}

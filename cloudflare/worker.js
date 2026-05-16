// @ts-check

/**
 * Cloudflare Containers – UruFactura API Worker
 *
 * This Worker is the entry point for all requests. It routes every incoming
 * request to the UruFactura .NET API container running alongside it.
 *
 * The container listens on port 8080 (ASPNETCORE_HTTP_PORTS=8080).
 * Cloudflare Containers uses Durable Objects to manage container lifecycle;
 * getContainer() returns a handle to the named instance.
 *
 * ### Single-tenant
 * All requests go to a single container named "default".
 * No X-Tenant-Id header is required.
 *
 * ### Multi-tenant (SaaS)
 * Send X-Tenant-Id: <tenantId> on every request.
 * Each tenant gets its own Durable Object → its own container → fully isolated
 * in-memory state (including CAEs).  Container env vars are shared across all
 * tenant containers, so configure each tenant under Tenants__<id>__* in the
 * wrangler.toml [vars] section or as Cloudflare Secrets.
 *
 * Docs: https://developers.cloudflare.com/containers/
 */

import { Container, getContainer } from "cloudflare:containers";

/**
 * UruFactura API container class.
 * Extending Container lets Cloudflare manage the container lifecycle
 * (start, stop, sleep) automatically.
 */
export class UruFacturaContainer extends Container {
  /** Port the .NET API listens on (matches ASPNETCORE_HTTP_PORTS in the Dockerfile). */
  defaultPort = 8080;

  /** Put the container to sleep after 5 minutes of inactivity to save resources. */
  sleepAfter = "5m";
}

export default {
  /**
   * Main fetch handler.
   *
   * Routes to a per-tenant container when X-Tenant-Id is present, or to the
   * shared "default" container for single-tenant deployments.
   *
   * @param {Request} request
   * @param {{ CONTAINER: DurableObjectNamespace }} env
   * @returns {Promise<Response>}
   */
  async fetch(request, env) {
    // Use X-Tenant-Id to select the Durable Object name.
    // Each unique name maps to a separate container instance with isolated memory.
    const tenantId = request.headers.get("X-Tenant-Id") ?? "default";
    const container = getContainer(env.CONTAINER, tenantId);
    return container.fetch(request);
  },
};

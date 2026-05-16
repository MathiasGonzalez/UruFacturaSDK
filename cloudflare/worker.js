// @ts-check

/**
 * Cloudflare Containers – UruFactura API Worker
 *
 * This Worker is the entry point for all requests. It routes every incoming
 * request to the UruFactura .NET API container running alongside it.
 *
 * The container listens on port 8080 (ASPNETCORE_HTTP_PORTS=8080).
 * Cloudflare Containers uses Durable Objects to manage container lifecycle;
 * getContainer() returns a handle to the single named instance.
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
   * Main fetch handler – forwards every request to the container.
   *
   * @param {Request} request
   * @param {{ CONTAINER: DurableObjectNamespace }} env
   * @returns {Promise<Response>}
   */
  async fetch(request, env) {
    // getContainer returns the singleton Durable Object for this Worker.
    // Cloudflare starts the container on the first request if it is asleep.
    const container = getContainer(env.CONTAINER);
    return container.fetch(request);
  },
};

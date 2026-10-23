import type { IncomingMessage } from "node:http";
import type { ProxyOptions } from "vite";

/**
 * Intermediario local del dashboard (perfil LocalDemo). La clave del
 * dashboard vive sólo en el proceso de Node de Vite (`AGROFLOW_DASHBOARD_KEY`,
 * sin prefijo VITE_): nunca llega al bundle, al navegador ni a su
 * almacenamiento. El proxy la agrega únicamente a las rutas de conversaciones
 * y transportistas, y sólo para solicitudes locales del mismo origen.
 *
 * Limitación: cualquier proceso o pestaña local que pueda hablar con el
 * servidor de Vite actúa como el dashboard. Es una medida de demo en
 * loopback, no una identidad de usuario (B03 la reemplaza por JWT).
 */

export const DASHBOARD_KEY_ENV = "AGROFLOW_DASHBOARD_KEY";
export const DASHBOARD_KEY_HEADER = "x-agroflow-dashboard-key";
const AGROFLOW_HEADERS = [DASHBOARD_KEY_HEADER, "x-agroflow-service-key"];

/** Rutas que el dashboard puede consultar con la clave. */
export const DASHBOARD_ROUTES = ["/api/v1/conversations", "/api/v1/drivers"];
/** Rutas de n8n: el navegador nunca las alcanza a través del proxy. */
export const CHANNEL_ROUTES = ["/api/v1/conversation-events", "/api/v1/conversation-reply-intents"];

type ProxyRequest = Pick<IncomingMessage, "headers" | "url"> & { socket: { remoteAddress?: string } };

export function isLoopbackAddress(address: string | undefined): boolean {
  if (!address) return false;
  const ipv4 = address.startsWith("::ffff:") ? address.slice(7) : address;
  return ipv4 === "::1" || /^127(?:\.\d{1,3}){3}$/.test(ipv4);
}

/** El Host debe ser local: evita que un dominio externo re-resuelto a 127.0.0.1 use el proxy. */
export function isLocalHost(host: string | undefined): boolean {
  if (!host) return false;
  return /^(?:localhost|127(?:\.\d{1,3}){3}|\[::1\])(?::\d{1,5})?$/i.test(host);
}

/**
 * Devuelve `true` si la solicitud puede recibir la clave del dashboard: cliente
 * de loopback, Host local, ruta sin segmentos de escape y, si el navegador
 * informa origen, el mismo origen (bloquea pedidos de otros sitios).
 */
export function isAllowedDashboardRequest(req: ProxyRequest): boolean {
  const host = req.headers.host;
  if (!isLoopbackAddress(req.socket.remoteAddress) || !isLocalHost(host)) return false;
  const url = req.url ?? "";
  if (/\.\.|%2e|%2f|%5c|\\/i.test(url)) return false;
  if (!DASHBOARD_ROUTES.some((route) => url === route || url.startsWith(`${route}/`) || url.startsWith(`${route}?`))) {
    return false;
  }
  const fetchSite = req.headers["sec-fetch-site"];
  if (fetchSite && fetchSite !== "same-origin" && fetchSite !== "none") return false;
  const origin = req.headers.origin;
  if (origin) {
    try {
      if (new URL(origin).host !== host) return false;
    } catch {
      return false;
    }
  }
  return true;
}

export function readDashboardKey(env: Record<string, string | undefined>): string | undefined {
  // Una clave con prefijo VITE_ quedaría embebida en el JavaScript del navegador.
  const exposed = Object.keys(env).filter((name) => name.startsWith("VITE_") && /KEY|SECRET|TOKEN/i.test(name));
  if (exposed.length > 0) {
    throw new Error(`No uses variables VITE_ para secretos (${exposed.join(", ")}): serían públicas en el navegador.`);
  }
  const key = env[DASHBOARD_KEY_ENV]?.trim();
  return key ? key : undefined;
}

function stripAgroFlowHeaders(proxyReq: { removeHeader(name: string): void }) {
  for (const header of AGROFLOW_HEADERS) proxyReq.removeHeader(header);
}

export function createApiProxy(target: string, dashboardKey: string | undefined): Record<string, ProxyOptions> {
  const base: ProxyOptions = {
    target,
    changeOrigin: true,
    // Si la API se levanta con el perfil https redirige a https://localhost:7138;
    // la redirección se sigue del lado del servidor y se acepta el certificado de desarrollo.
    followRedirects: true,
    secure: false,
  };

  const blocked: ProxyOptions = { ...base, bypass: () => false };

  const dashboard: ProxyOptions = {
    ...base,
    // `false` hace que Vite responda 404 sin reenviar la solicitud.
    bypass: (req) => (isAllowedDashboardRequest(req as ProxyRequest) ? undefined : false),
    configure: (proxy) => {
      proxy.on("proxyReq", (proxyReq) => {
        stripAgroFlowHeaders(proxyReq);
        if (dashboardKey) proxyReq.setHeader(DASHBOARD_KEY_HEADER, dashboardKey);
      });
    },
  };

  const general: ProxyOptions = {
    ...base,
    configure: (proxy) => {
      proxy.on("proxyReq", (proxyReq) => stripAgroFlowHeaders(proxyReq));
    },
  };

  // El orden importa: Vite usa la primera clave que es prefijo de la URL.
  return {
    ...Object.fromEntries(CHANNEL_ROUTES.map((route) => [route, blocked])),
    ...Object.fromEntries(DASHBOARD_ROUTES.map((route) => [route, dashboard])),
    "/api": general,
    "/health": general,
  };
}

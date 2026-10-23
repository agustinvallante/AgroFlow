import { describe, expect, it } from "vitest";
import {
  CHANNEL_ROUTES,
  DASHBOARD_KEY_HEADER,
  createApiProxy,
  isAllowedDashboardRequest,
  isLocalHost,
  isLoopbackAddress,
  readDashboardKey,
} from "./dashboardProxy.ts";

const local = (url: string, headers: Record<string, string> = {}, remoteAddress = "127.0.0.1") => ({
  url,
  headers: { host: "localhost:5173", ...headers },
  socket: { remoteAddress },
});

describe("intermediario local del dashboard", () => {
  it("reconoce sólo direcciones y hosts de loopback", () => {
    expect(isLoopbackAddress("127.0.0.1")).toBe(true);
    expect(isLoopbackAddress("::1")).toBe(true);
    expect(isLoopbackAddress("::ffff:127.0.0.1")).toBe(true);
    expect(isLoopbackAddress("192.168.0.10")).toBe(false);
    expect(isLoopbackAddress(undefined)).toBe(false);
    expect(isLocalHost("localhost:5173")).toBe(true);
    expect(isLocalHost("127.0.0.1:4173")).toBe(true);
    expect(isLocalHost("[::1]:5173")).toBe(true);
    expect(isLocalHost("attacker.example:5173")).toBe(false);
    expect(isLocalHost("localhost.attacker.example")).toBe(false);
  });

  it("permite las rutas del dashboard desde el mismo origen local", () => {
    expect(isAllowedDashboardRequest(local("/api/v1/conversations"))).toBe(true);
    expect(isAllowedDashboardRequest(local("/api/v1/conversations?limit=20"))).toBe(true);
    expect(isAllowedDashboardRequest(local("/api/v1/drivers", { origin: "http://localhost:5173", "sec-fetch-site": "same-origin" }))).toBe(true);
  });

  it("rechaza clientes remotos, hosts externos, otros orígenes y rutas con escapes", () => {
    expect(isAllowedDashboardRequest(local("/api/v1/drivers", {}, "10.0.0.5"))).toBe(false);
    expect(isAllowedDashboardRequest(local("/api/v1/drivers", { host: "rebind.attacker.example" }))).toBe(false);
    expect(isAllowedDashboardRequest(local("/api/v1/drivers", { origin: "https://attacker.example" }))).toBe(false);
    expect(isAllowedDashboardRequest(local("/api/v1/drivers", { "sec-fetch-site": "cross-site" }))).toBe(false);
    expect(isAllowedDashboardRequest(local("/api/v1/drivers/../conversation-events"))).toBe(false);
    expect(isAllowedDashboardRequest(local("/api/v1/drivers/%2e%2e/conversation-events"))).toBe(false);
    expect(isAllowedDashboardRequest(local("/api/v1/driversX"))).toBe(false);
  });

  it("bloquea las rutas del canal y sólo agrega la clave en las del dashboard", () => {
    const proxy = createApiProxy("http://localhost:5000", "clave-de-prueba-sintetica-0123456789");
    for (const route of CHANNEL_ROUTES) {
      expect(proxy[route].bypass?.(local(route) as never, undefined, proxy[route] as never)).toBe(false);
    }
    expect(Object.keys(proxy).slice(0, 4)).toEqual([...CHANNEL_ROUTES, "/api/v1/conversations", "/api/v1/drivers"]);

    const headers = new Map<string, string>([[DASHBOARD_KEY_HEADER, "valor-del-navegador"], ["x-agroflow-service-key", "x"]]);
    const proxyReq = {
      removeHeader: (name: string) => {
        headers.delete(name);
      },
      setHeader: (name: string, value: string) => {
        headers.set(name, value);
      },
    };
    const listeners: Record<string, (request: typeof proxyReq) => void> = {};
    proxy["/api/v1/drivers"].configure?.({ on: (event: string, fn: never) => (listeners[event] = fn) } as never, {} as never);
    listeners.proxyReq(proxyReq);
    expect(headers.get(DASHBOARD_KEY_HEADER)).toBe("clave-de-prueba-sintetica-0123456789");
    expect(headers.has("x-agroflow-service-key")).toBe(false);

    const general = new Map<string, string>([[DASHBOARD_KEY_HEADER, "valor-del-navegador"]]);
    proxy["/api"].configure?.({ on: (_event: string, fn: never) => (listeners.general = fn) } as never, {} as never);
    listeners.general({ removeHeader: (name: string) => { general.delete(name); }, setHeader: () => {} });
    expect(general.size).toBe(0);
  });

  it("lee la clave sin prefijo VITE_ y rechaza secretos públicos", () => {
    expect(readDashboardKey({ AGROFLOW_DASHBOARD_KEY: "  clave  " })).toBe("clave");
    expect(readDashboardKey({})).toBeUndefined();
    expect(() => readDashboardKey({ VITE_DASHBOARD_KEY: "x" })).toThrow(/VITE_/);
  });
});

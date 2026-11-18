import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { HttpVentanaRepository } from "./HttpVentanaRepository";
import { ObtenerVentanas } from "../../../application/usecases/ventanas/ObtenerVentanas";

describe("lectura HTTP de ventanas", () => {
  const fetchMock = vi.fn<typeof fetch>();
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });
  afterEach(() => vi.unstubAllGlobals());

  it("consulta hoy sin date y conserva todos los campos de Window sin secretos del navegador", async () => {
    const datos = [{ id: "v1", startAt: "2026-09-28T23:15:00-03:00", endAt: "2026-09-29T00:00:00-03:00", capacity: 7, occupied: 2, available: 5 }];
    fetchMock.mockResolvedValue(new Response(JSON.stringify(datos), { status: 200 }));
    const caso = new ObtenerVentanas(new HttpVentanaRepository(""));
    expect(await caso.execute()).toEqual(datos);
    const [url, init] = fetchMock.mock.calls[0];
    expect(new URL(String(url)).pathname).toBe("/api/v1/windows");
    expect(new URL(String(url)).search).toBe("");
    expect(init?.method).toBe("GET");
    expect(JSON.stringify(init?.headers)).not.toMatch(/agroflow|key|token/i);
  });

  it("envía sólo la fecha elegida y conserva un día vacío como arreglo vacío", async () => {
    fetchMock.mockResolvedValue(new Response("[]", { status: 200 }));
    expect(await new HttpVentanaRepository("").obtenerPorFecha("2026-09-29")).toEqual([]);
    expect(new URL(String(fetchMock.mock.calls[0][0])).search).toBe("?date=2026-09-29");
  });

  it("propaga un 401 como error accionable, nunca como cupo cero", async () => {
    fetchMock.mockResolvedValue(new Response(JSON.stringify({ code: "UNAUTHENTICATED" }), { status: 401 }));
    await expect(new HttpVentanaRepository("").obtenerPorFecha()).rejects.toThrow(/AGROFLOW_DASHBOARD_KEY/);
  });
});

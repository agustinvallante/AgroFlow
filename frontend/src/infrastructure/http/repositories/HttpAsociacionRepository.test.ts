import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { HttpAsociacionRepository } from "./HttpAsociacionRepository";
import { ObtenerFlotaAsociada } from "../../../application/usecases/transportistas/AsociacionUseCases";

const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status });

describe("asociaciones HTTP transportista-camión", () => {
  const fetchMock = vi.fn<typeof fetch>();
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });
  afterEach(() => vi.unstubAllGlobals());

  it("agrupa los camiones activos por transportista y deja libres los que no tienen asociación", async () => {
    fetchMock.mockImplementation(async (url) => {
      const { pathname, search } = new URL(String(url));
      expect(search).toBe("?active=true");
      if (pathname === "/api/v1/trucks") {
        return json([
          { id: "c1", plate: "AF123BC", fleetType: "PROPIA", isActive: true },
          { id: "c2", plate: "AG888AA", fleetType: "TERCEROS", isActive: true },
          { id: "c3", plate: "AA111AA", fleetType: "PROPIA", isActive: true },
        ]);
      }
      return json([
        { id: "a1", driverId: "d1", truckId: "c1", isActive: true },
        { id: "a2", driverId: "d1", truckId: "c3", isActive: true },
        { id: "a3", driverId: "d2", truckId: "inhabilitado", isActive: true },
      ]);
    });

    const flota = await new ObtenerFlotaAsociada(new HttpAsociacionRepository("")).execute();

    expect(flota.porTransportista.d1).toEqual([
      { asociacionId: "a2", camionId: "c3", patente: "AA111AA" },
      { asociacionId: "a1", camionId: "c1", patente: "AF123BC" },
    ]);
    expect(flota.porTransportista.d2).toBeUndefined();
    expect(flota.libres.map((c) => c.patente)).toEqual(["AG888AA"]);
  });

  it("asocia y desasocia con una Idempotency-Key nueva en cada mutación", async () => {
    fetchMock.mockImplementation(async () => json({ resourceId: "a9", resourceType: "association", outcome: "created" }, 201));
    let n = 0;
    const repo = new HttpAsociacionRepository("", () => `clave-${++n}`);

    await repo.asociar("d1", "c2");
    await repo.desasociar("a9");

    const [urlAlta, alta] = fetchMock.mock.calls[0];
    expect(String(urlAlta)).toBe("/api/v1/associations");
    expect(alta?.method).toBe("POST");
    expect(JSON.parse(String(alta?.body))).toEqual({ driverId: "d1", truckId: "c2" });
    expect((alta?.headers as Record<string, string>)["Idempotency-Key"]).toBe("clave-1");
    const [urlBaja, baja] = fetchMock.mock.calls[1];
    expect(String(urlBaja)).toBe("/api/v1/associations/a9/inactivation");
    expect(baja?.body).toBeUndefined();
    expect((baja?.headers as Record<string, string>)["Idempotency-Key"]).toBe("clave-2");
  });

  it("propaga el rechazo del backend cuando el camión ya tiene transportista", async () => {
    fetchMock.mockResolvedValue(json({ code: "DUPLICATE_ASSOCIATION", title: "El camión ya tiene un transportista activo" }, 409));

    await expect(new HttpAsociacionRepository("").asociar("d1", "c1")).rejects.toThrow("El camión ya tiene un transportista activo");
  });
});

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { HttpTurnoRepository } from "./HttpTurnoRepository";
import { ApiError } from "../ApiClient";
import type { AppointmentDto } from "../dto/AppointmentDto";

const turnoApi: AppointmentDto = {
  id: "8f7c1b2a-0000-4000-8000-000000000001",
  carrier: { id: "c1", name: "Juan Pérez" },
  truck: { id: "t1", plate: "AF123BC" },
  farm: { id: "f1", name: "Finca Norte" },
  cutAt: "2026-09-28T05:30:00-03:00",
  estimatedLoadTons: 28.5,
  window: { startAt: "2026-09-28T08:00:00-03:00", endAt: "2026-09-28T08:30:00-03:00" },
  status: "EN_ESPERA",
  createdAt: "2026-09-28T06:00:00-03:00",
};

const respuesta = (status: number, body: unknown, contentType = "application/json") =>
  new Response(JSON.stringify(body), { status, headers: { "content-type": contentType } });

describe("HttpTurnoRepository.cambiarEstado", () => {
  const fetchMock = vi.fn<typeof fetch>();

  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("envía el estado elegido por el operador sin consultar antes el estado vigente", async () => {
    fetchMock.mockResolvedValueOnce(respuesta(200, turnoApi));

    const turno = await new HttpTurnoRepository("").cambiarEstado(turnoApi.id, "cancha");

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe(`/api/v1/appointments/${turnoApi.id}/transitions`);
    expect(init?.method).toBe("POST");
    expect(JSON.parse(String(init?.body))).toEqual({ newStatus: "EN_ESPERA" });
    expect(turno.estado).toBe("cancha");
  });

  it("traduce cada estado del dominio al valor del contrato", async () => {
    const casos = [
      ["pendiente", "ASIGNADO"],
      ["viaje", "EN_CAMINO"],
      ["cancha", "EN_ESPERA"],
      ["ingresado", "INGRESADO"],
      ["descargando", "EN_DESCARGA"],
      ["completado", "FINALIZADO"],
      ["cancelado", "CANCELADO"],
    ] as const;
    const repo = new HttpTurnoRepository("");

    for (const [dominio, contrato] of casos) {
      fetchMock.mockResolvedValueOnce(respuesta(200, { ...turnoApi, status: contrato }));
      await repo.cambiarEstado(turnoApi.id, dominio);
      expect(JSON.parse(String(fetchMock.mock.lastCall?.[1]?.body))).toEqual({ newStatus: contrato });
    }
  });

  it("propaga el rechazo de la API (409) sin decidir nada localmente", async () => {
    fetchMock.mockResolvedValueOnce(
      respuesta(
        409,
        { type: "about:blank", title: "Transición inválida", status: 409, code: "INVALID_TRANSITION", traceId: "t" },
        "application/problem+json"
      )
    );

    const error = await new HttpTurnoRepository("").cambiarEstado(turnoApi.id, "completado").catch((e) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(409);
    expect(error.code).toBe("INVALID_TRANSITION");
  });

  it("rechaza sin llamar a la API un estado que el contrato no tiene", async () => {
    await expect(new HttpTurnoRepository("").cambiarEstado(turnoApi.id, "demorado")).rejects.toThrow();
    expect(fetchMock).not.toHaveBeenCalled();
  });
});

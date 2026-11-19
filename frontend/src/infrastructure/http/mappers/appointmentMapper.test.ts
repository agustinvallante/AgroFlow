import { afterEach, describe, expect, it, vi } from "vitest";
import { combinarResumenYDetalle, mapAppointment } from "./appointmentMapper";
import type { AppointmentDto } from "../dto/AppointmentDto";

const dto: AppointmentDto = {
  id: "8f7c1b2a-0000-4000-8000-000000000001",
  carrier: { id: "c1", name: "Juan Pérez" },
  truck: { id: "t1", plate: "AF123BC" },
  farm: { id: "f1", name: "Finca Norte" },
  cutAt: "2026-09-28T05:30:00-03:00",
  estimatedLoadTons: 28.5,
  window: { startAt: "2026-09-28T23:30:00-03:00", endAt: "2026-09-29T00:00:00-03:00" },
  status: "INGRESADO",
  createdAt: "2026-09-28T06:00:00-03:00",
};

describe("mapAppointment", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("muestra la ventana en la zona del ingenio informada por la API", () => {
    const turno = mapAppointment(dto);
    expect(turno.hora).toBe("23:30");
    expect(turno.ventanaFin).toBe("00:00");
    expect(turno.ventana).toEqual({ inicio: dto.window.startAt, fin: dto.window.endAt });
  });

  it("no depende de la zona horaria del navegador", () => {
    // Simula un navegador en otra zona: si el mapper convirtiera con Date,
    // mostraría estas horas en lugar de las que informa la API.
    vi.spyOn(Date.prototype, "getHours").mockReturnValue(11);
    vi.spyOn(Date.prototype, "getMinutes").mockReturnValue(45);
    vi.spyOn(Date.prototype, "getTimezoneOffset").mockReturnValue(-540);

    const turno = mapAppointment(dto);
    expect(turno.hora).toBe("23:30");
    expect(turno.ventanaFin).toBe("00:00");
  });

  it("no inventa datos que la API no informa", () => {
    const turno = mapAppointment(dto);
    expect(turno.estado).toBe("ingresado");
    for (const campo of ["prioridad", "flota", "canal", "esperaMin"]) {
      expect(turno).not.toHaveProperty(campo);
    }
  });

  it("conserva la fecha y ventana más reciente del resumen sobre el detalle cacheado", () => {
    const window = { startAt: "2026-09-30T08:00:00-03:00", endAt: "2026-09-30T08:30:00-03:00" };
    const turno = combinarResumenYDetalle({ id: dto.id, truck: dto.truck, window, status: "EN_CAMINO" }, dto);
    expect(turno.ventana).toEqual({ inicio: window.startAt, fin: window.endAt });
    expect(turno.estado).toBe("viaje");
  });

  it("traduce el incidente abierto que informa la API", () => {
    const turno = mapAppointment({
      ...dto,
      status: "EN_CAMINO",
      openIncident: {
        id: "i1",
        type: "FALLA_MECANICA",
        canContinue: false,
        estimatedDelayMinutes: 45,
        description: "Se cortó la correa",
        reportedAt: "2026-09-28T22:10:00-03:00",
      },
    });
    expect(turno.incidente).toEqual({
      id: "i1",
      tipo: "falla-mecanica",
      puedeSeguir: false,
      demoraEstimadaMin: 45,
      detalle: "Se cortó la correa",
      reportadoEn: "2026-09-28T22:10:00-03:00",
    });
    // El incidente no cambia el estado del turno.
    expect(turno.estado).toBe("viaje");
  });

  it("sin incidente abierto, o con uno desconocido, no inventa datos", () => {
    expect(mapAppointment({ ...dto, openIncident: null }).incidente).toBeUndefined();
    expect(mapAppointment(dto).incidente).toBeUndefined();
    const incidente = { id: "i2", canContinue: true, estimatedDelayMinutes: 10, description: null, reportedAt: dto.createdAt };
    const raro = mapAppointment({ ...dto, openIncident: { ...incidente, type: "INUNDACION" as never } });
    expect(raro.incidente?.tipo).toBe("otro");
  });

  it("toma el incidente del resumen, la lectura más reciente, sobre el del detalle cacheado", () => {
    const abierto = {
      id: "i1",
      type: "PINCHAZO" as const,
      canContinue: true,
      estimatedDelayMinutes: 30,
      description: null,
      reportedAt: "2026-09-28T22:10:00-03:00",
    };
    const resumen = { id: dto.id, truck: dto.truck, window: dto.window, status: "EN_CAMINO" as const };
    // Resuelto: el resumen trae null aunque el detalle cacheado lo tenía abierto.
    expect(combinarResumenYDetalle({ ...resumen, openIncident: null }, { ...dto, openIncident: abierto }).incidente).toBeUndefined();
    expect(combinarResumenYDetalle({ ...resumen, openIncident: abierto }, { ...dto, openIncident: null }).incidente?.tipo).toBe("pinchazo");
  });
});

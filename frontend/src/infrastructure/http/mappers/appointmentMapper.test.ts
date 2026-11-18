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
});

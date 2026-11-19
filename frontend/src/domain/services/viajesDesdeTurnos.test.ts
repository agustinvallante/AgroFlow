import { describe, expect, it } from "vitest";
import type { EstadoTurno, Turno } from "../entities/Turno";
import { calcularViajeDeTurno, fincaDelTurno } from "./viajesDesdeTurnos";

const ingenio = { lat: -26.83, lon: -65.2 };
const finca = { id: "f1", nombre: "Finca Norte", lat: -26.65, lon: -65.2 };
const turno = (estado: EstadoTurno): Turno => ({
  id: "t1",
  ventana: { inicio: "2026-11-08T16:00:00Z", fin: "2026-11-08T16:30:00Z" },
  hora: "13:00",
  offsetMin: 0,
  patente: "AF123BC",
  chofer: "Juan Pérez",
  finca: "finca norte",
  horasDesdeCorte: 1,
  estado,
});
const inicio = Date.parse("2026-11-08T16:00:00Z");

describe("viajes desde la cola de turnos", () => {
  it("busca la finca por nombre y llega al inicio de la ventana", () => {
    const viaje = calcularViajeDeTurno(turno("pendiente"), fincaDelTurno(turno("pendiente"), [finca])!, ingenio, inicio - 1);

    expect(viaje?.llegadaEstimadaMs).toBe(inicio);
    expect(viaje?.situacion.estado).toBe("en-ruta");
    expect(calcularViajeDeTurno(turno("pendiente"), finca, ingenio, inicio + 60_000)?.situacion.estado).toBe("en-playa");
  });

  it("toma la llegada que informa la API y descarta los turnos terminados", () => {
    for (const estado of ["cancha", "ingresado", "descargando"] as const) {
      expect(calcularViajeDeTurno(turno(estado), finca, ingenio, inicio - 3_600_000)?.situacion.estado).toBe("en-playa");
    }
    for (const estado of ["completado", "cancelado"] as const) {
      expect(calcularViajeDeTurno(turno(estado), finca, ingenio, inicio)).toBeNull();
    }
  });
});

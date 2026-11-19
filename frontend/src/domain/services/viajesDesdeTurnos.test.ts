import { describe, expect, it } from "vitest";
import type { EstadoTurno, Turno } from "../entities/Turno";
import { calcularViajeDeTurno, fincaDelTurno } from "./viajesDesdeTurnos";
import { PROGRESO_MAXIMO_SIN_CONFIRMAR } from "./simulacionViajes";

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

  describe("con un problema en ruta", () => {
    const enViaje = turno("viaje");
    // Recorrido sin incidente: sale del inicio de la ventana menos la duración estimada.
    const salidaMs = calcularViajeDeTurno(enViaje, finca, ingenio, inicio)!.viaje.salidaMs;
    const reportadoMs = salidaMs + (inicio - salidaMs) / 2;
    const conIncidente: Turno = {
      ...enViaje,
      incidente: {
        id: "i1",
        tipo: "pinchazo",
        puedeSeguir: true,
        demoraEstimadaMin: 30,
        detalle: null,
        reportadoEn: new Date(reportadoMs).toISOString(),
      },
    };

    it("figura con demora y queda donde estaba al reportarlo", () => {
      const alReportar = calcularViajeDeTurno(enViaje, finca, ingenio, reportadoMs)!.situacion.progreso;
      expect(alReportar).toBeGreaterThan(0);

      for (const ahoraMs of [reportadoMs + 60_000, inicio - 60_000, inicio + 3_600_000]) {
        const viaje = calcularViajeDeTurno(conIncidente, finca, ingenio, ahoraMs)!;
        expect(viaje.situacion.estado).toBe("demora");
        expect(viaje.situacion.progreso).toBeCloseTo(alReportar);
        expect(viaje.situacion.progreso).toBeLessThanOrEqual(PROGRESO_MAXIMO_SIN_CONFIRMAR);
        expect(viaje.turno?.incidente).toEqual(conIncidente.incidente);
        // El estado de la API no cambia por el incidente.
        expect(viaje.turno?.estado).toBe("viaje");
      }
    });

    it("con el reloj del mapa adelantado queda donde estaba cuando el mapa vio el incidente", () => {
      // El mapa acelerado vio el reporte cuando su reloj ya iba por detrás de la llegada, no en la hora real.
      const vistoEnMs = inicio - 120_000;
      const mostrado = calcularViajeDeTurno(enViaje, finca, ingenio, vistoEnMs)!.situacion.progreso;
      expect(mostrado).toBeGreaterThan(calcularViajeDeTurno(enViaje, finca, ingenio, reportadoMs)!.situacion.progreso);

      const viaje = calcularViajeDeTurno(conIncidente, finca, ingenio, inicio + 3_600_000, vistoEnMs)!;
      expect(viaje.situacion).toEqual({ estado: "demora", progreso: Math.min(mostrado, PROGRESO_MAXIMO_SIN_CONFIRMAR) });
      // Nunca llega sin confirmación, aunque el mapa lo viera después de la llegada estimada.
      const tarde = calcularViajeDeTurno(conIncidente, finca, ingenio, inicio + 7_200_000, inicio + 3_600_000)!;
      expect(tarde.situacion.progreso).toBe(PROGRESO_MAXIMO_SIN_CONFIRMAR);
    });

    it("si la API ya lo informa en el ingenio, figura como llegado", () => {
      for (const estado of ["cancha", "ingresado", "descargando"] as const) {
        const viaje = calcularViajeDeTurno({ ...conIncidente, estado }, finca, ingenio, reportadoMs + 60_000)!;
        expect(viaje.situacion).toEqual({ estado: "en-playa", progreso: 1 });
      }
    });

    it("sin incidente avanza como siempre", () => {
      const viaje = calcularViajeDeTurno(enViaje, finca, ingenio, reportadoMs + 60_000)!;
      expect(viaje.situacion.estado).toBe("en-ruta");
      expect(viaje.situacion.progreso).toBeGreaterThan(calcularViajeDeTurno(enViaje, finca, ingenio, reportadoMs)!.situacion.progreso);
      expect(viaje.turno).not.toHaveProperty("incidente");
    });
  });
});

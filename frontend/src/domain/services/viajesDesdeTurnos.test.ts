import { describe, expect, it } from "vitest";
import type { EstadoTurno, IncidenteEnRuta, Turno } from "../entities/Turno";
import { calcularViajeDeTurno, fincaDelTurno, VELOCIDAD_TURNOS_KMH } from "./viajesDesdeTurnos";
import { distanciaRutaKm, duracionEstimadaMs, PROGRESO_MAXIMO_SIN_CONFIRMAR } from "./simulacionViajes";

const ingenio = { lat: -26.83, lon: -65.2 };
const finca = { id: "f1", nombre: "Finca Norte", lat: -26.65, lon: -65.2 };
const MINUTO = 60_000;
const HORA = 60 * MINUTO;
const inicio = Date.parse("2026-11-08T16:00:00Z");
const salida = Date.parse("2026-11-08T15:00:00Z");
const duracion = duracionEstimadaMs(distanciaRutaKm(finca, ingenio), VELOCIDAD_TURNOS_KMH);
/** Primer milisegundo entero en que se cumple la duración del recorrido, a salvo del redondeo. */
const llegada = Math.ceil(salida + duracion);
const iso = (ms: number) => new Date(ms).toISOString();

const turno = (estado: EstadoTurno, extra: Partial<Turno> = {}): Turno => ({
  id: "t1",
  ventana: { inicio: iso(inicio), fin: "2026-11-08T16:30:00Z" },
  hora: "13:00",
  offsetMin: 0,
  patente: "AF123BC",
  chofer: "Juan Pérez",
  finca: "finca norte",
  horasDesdeCorte: 1,
  estado,
  ...extra,
});
const enViaje = (extra: Partial<Turno> = {}) => turno("viaje", { salidaEn: iso(salida), ...extra });
const incidente = (reportadoMs: number): IncidenteEnRuta => ({
  id: "i1",
  tipo: "pinchazo",
  puedeSeguir: true,
  demoraEstimadaMin: 30,
  detalle: null,
  reportadoEn: iso(reportadoMs),
});
const situacion = (t: Turno, ahoraMs: number, reloj?: Parameters<typeof calcularViajeDeTurno>[4]) =>
  calcularViajeDeTurno(t, finca, ingenio, ahoraMs, reloj)!.situacion;

describe("viajes desde la cola de turnos", () => {
  it("busca la finca por nombre", () => {
    expect(fincaDelTurno(turno("pendiente"), [finca])).toBe(finca);
  });

  it("un turno asignado queda en su finca como programado, aunque pase su ventana", () => {
    for (const ahoraMs of [inicio - 2 * HORA, inicio, inicio + 5 * HORA]) {
      expect(situacion(turno("pendiente"), ahoraMs)).toEqual({ estado: "programado", progreso: 0 });
    }
  });

  it("un turno en camino sale a la hora que informa la API", () => {
    const viaje = calcularViajeDeTurno(enViaje(), finca, ingenio, salida + duracion / 2)!;
    expect(viaje.viaje.salidaMs).toBe(salida);
    expect(viaje.llegadaEstimadaMs).toBe(salida + duracion);
    expect(viaje.situacion.estado).toBe("en-ruta");
    expect(viaje.situacion.progreso).toBeCloseTo(PROGRESO_MAXIMO_SIN_CONFIRMAR / 2);
  });

  it("sin salida informada sale cuando el mapa lo vio en camino por primera vez", () => {
    const vistoMs = salida + 10 * MINUTO;
    const viaje = calcularViajeDeTurno(turno("viaje"), finca, ingenio, vistoMs + duracion / 2, {
      aHoraMapa: (ms) => ms,
      salidaVistaMs: vistoMs,
    })!;
    expect(viaje.viaje.salidaMs).toBe(vistoMs);
    expect(viaje.situacion.progreso).toBeCloseTo(PROGRESO_MAXIMO_SIN_CONFIRMAR / 2);
  });

  it("convierte la salida real a la hora del mapa", () => {
    const adelanto = 2 * HORA;
    const viaje = calcularViajeDeTurno(enViaje(), finca, ingenio, salida + adelanto + duracion / 4, {
      aHoraMapa: (ms) => ms + adelanto,
    })!;
    expect(viaje.viaje.salidaMs).toBe(salida + adelanto);
    expect(viaje.situacion.progreso).toBeCloseTo(PROGRESO_MAXIMO_SIN_CONFIRMAR / 4);
  });

  it("al llegar a la hora estimada espera en la puerta: no figura llegado sin que lo informe la API", () => {
    for (const ahoraMs of [llegada, llegada + 3 * HORA]) {
      expect(situacion(enViaje(), ahoraMs)).toEqual({ estado: "en-puerta", progreso: PROGRESO_MAXIMO_SIN_CONFIRMAR });
    }
  });

  it("toma la llegada que informa la API y descarta los turnos terminados", () => {
    for (const estado of ["cancha", "ingresado", "descargando"] as const) {
      expect(situacion(turno(estado), inicio - HORA)).toEqual({ estado: "en-playa", progreso: 1 });
      expect(situacion(turno(estado, { salidaEn: iso(salida) }), salida + 1)).toEqual({ estado: "en-playa", progreso: 1 });
    }
    for (const estado of ["completado", "cancelado"] as const) {
      expect(calcularViajeDeTurno(turno(estado), finca, ingenio, inicio)).toBeNull();
    }
  });

  describe("con un problema en ruta", () => {
    const reportadoMs = salida + duracion / 2;

    it("figura con demora y queda donde estaba al reportarlo mientras siga abierto", () => {
      const conIncidente = enViaje({ incidente: incidente(reportadoMs) });
      for (const ahoraMs of [reportadoMs + MINUTO, reportadoMs + duracion, reportadoMs + 5 * HORA]) {
        const viaje = calcularViajeDeTurno(conIncidente, finca, ingenio, ahoraMs)!;
        expect(viaje.situacion.estado).toBe("demora");
        expect(viaje.situacion.progreso).toBeCloseTo(PROGRESO_MAXIMO_SIN_CONFIRMAR / 2);
        expect(viaje.turno?.incidente).toEqual(conIncidente.incidente);
        // El estado de la API no cambia por el incidente.
        expect(viaje.turno?.estado).toBe("viaje");
      }
    });

    it("al resolverse sigue desde el mismo punto y llega más tarde", () => {
      const resueltoMs = reportadoMs + 40 * MINUTO;
      const abierto = calcularViajeDeTurno(enViaje({ incidente: incidente(reportadoMs) }), finca, ingenio, resueltoMs)!;
      const resuelto = enViaje({ paradas: [{ inicio: iso(reportadoMs), fin: iso(resueltoMs) }] });

      const alResolver = calcularViajeDeTurno(resuelto, finca, ingenio, resueltoMs)!;
      expect(alResolver.situacion.estado).toBe("en-ruta");
      expect(alResolver.situacion.progreso).toBeCloseTo(abierto.situacion.progreso);
      expect(alResolver.llegadaEstimadaMs).toBe(salida + duracion + 40 * MINUTO);
      expect(situacion(resuelto, salida + duracion + 39 * MINUTO).estado).toBe("en-ruta");
      expect(situacion(resuelto, llegada + 40 * MINUTO).estado).toBe("en-puerta");
    });

    it("suma todas las paradas resueltas", () => {
      const paradas = [
        { inicio: iso(salida + 5 * MINUTO), fin: iso(salida + 15 * MINUTO) },
        { inicio: iso(salida + 20 * MINUTO), fin: iso(salida + 25 * MINUTO) },
      ];
      expect(calcularViajeDeTurno(enViaje({ paradas }), finca, ingenio, salida + 30 * MINUTO)!.llegadaEstimadaMs).toBe(
        salida + duracion + 15 * MINUTO,
      );
    });

    it("con el reloj acelerado se detiene en la hora del mapa del reporte y reanuda sin saltos", () => {
      // Reloj del mapa a x60 desde la salida: cada minuto real son 60 del mapa.
      const aHoraMapa = (ms: number) => salida + (ms - salida) * 60;
      const reporteReal = salida + 15_000;
      const finReal = salida + 45_000;
      const detenido = situacion(enViaje({ incidente: incidente(reporteReal) }), aHoraMapa(finReal), { aHoraMapa });
      expect(detenido.progreso).toBeCloseTo(((15 * MINUTO) / duracion) * PROGRESO_MAXIMO_SIN_CONFIRMAR);

      const resuelto = enViaje({ paradas: [{ inicio: iso(reporteReal), fin: iso(finReal) }] });
      expect(situacion(resuelto, aHoraMapa(finReal), { aHoraMapa }).progreso).toBeCloseTo(detenido.progreso);
    });

    it("si el mapa vio el incidente antes que la hora convertida del reporte, se detiene en la que vio", () => {
      const vistoMs = reportadoMs - 5 * MINUTO;
      const viaje = situacion(enViaje({ incidente: incidente(reportadoMs) }), reportadoMs + HORA, {
        aHoraMapa: (ms) => ms,
        detenidoVistoMs: vistoMs,
      });
      expect(viaje.progreso).toBeCloseTo(((vistoMs - salida) / duracion) * PROGRESO_MAXIMO_SIN_CONFIRMAR);
    });

    it("si la API ya lo informa en el ingenio, figura como llegado", () => {
      for (const estado of ["cancha", "ingresado", "descargando"] as const) {
        const viaje = calcularViajeDeTurno(
          { ...enViaje({ incidente: incidente(reportadoMs) }), estado },
          finca,
          ingenio,
          reportadoMs + MINUTO,
        )!;
        expect(viaje.situacion).toEqual({ estado: "en-playa", progreso: 1 });
      }
    });
  });
});

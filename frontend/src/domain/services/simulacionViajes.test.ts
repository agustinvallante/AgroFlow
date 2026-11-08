import { describe, expect, it } from "vitest";
import {
  calcularSituacion,
  calcularViaje,
  distanciaRutaKm,
  duracionEstimadaMs,
  FACTOR_RUTA,
  PROGRESO_MAXIMO_SIN_CONFIRMAR,
} from "./simulacionViajes";

const MINUTO = 60_000;

describe("distancia y duración", () => {
  it("aplica el factor de ruta a la distancia en línea recta", () => {
    // Un grado de latitud son unos 111,2 km.
    expect(distanciaRutaKm({ lat: -26, lon: -65 }, { lat: -27, lon: -65 })).toBeCloseTo(111.19 * FACTOR_RUTA, 0);
  });

  it("estima la duración con la velocidad", () => {
    expect(duracionEstimadaMs(40, 40)).toBe(60 * MINUTO);
  });
});

describe("calcularSituacion", () => {
  const salida = 1_000_000;
  const duracion = 60 * MINUTO;
  const llegada = salida + 90 * MINUTO;

  it("no arranca antes de la salida", () => {
    expect(calcularSituacion(salida, duracion, llegada, salida - 1)).toEqual({ estado: "programado", progreso: 0 });
  });

  it("avanza en proporción al tiempo transcurrido", () => {
    const situacion = calcularSituacion(salida, duracion, llegada, salida + 30 * MINUTO);
    expect(situacion.estado).toBe("en-ruta");
    expect(situacion.progreso).toBeCloseTo(PROGRESO_MAXIMO_SIN_CONFIRMAR / 2);
  });

  it("sin retraso recorre toda la ruta hasta llegar", () => {
    const situacion = calcularSituacion(salida, duracion, salida + duracion, salida + 30 * MINUTO);
    expect(situacion.progreso).toBeCloseTo(0.5);
  });

  it("pasa a demora al superar la estimación y espera cerca del ingenio", () => {
    expect(calcularSituacion(salida, duracion, llegada, salida + 75 * MINUTO)).toEqual({
      estado: "demora",
      progreso: PROGRESO_MAXIMO_SIN_CONFIRMAR,
    });
  });

  it("sin llegada confirmada sigue en demora indefinidamente", () => {
    expect(calcularSituacion(salida, duracion, null, salida + 600 * MINUTO).estado).toBe("demora");
  });

  it("queda en playa cuando llega", () => {
    expect(calcularSituacion(salida, duracion, llegada, llegada)).toEqual({ estado: "en-playa", progreso: 1 });
  });

  it("no divide por cero si la finca coincide con el ingenio", () => {
    expect(calcularSituacion(salida, 0, null, salida + 1).estado).toBe("demora");
  });
});

describe("calcularViaje", () => {
  it("suma el retraso a la llegada real, no a la estimada", () => {
    const finca = { id: "f", nombre: "F", lat: -26, lon: -65 };
    const ingenio = { lat: -26.5, lon: -65 };
    const viaje = { id: "v", patente: "AF100AA", fincaId: "f", salidaMs: 0, velocidadKmh: 40, retrasoMin: 20 };
    const sinRetraso = calcularViaje({ ...viaje, retrasoMin: 0 }, finca, ingenio, 0);

    const durante = calcularViaje(viaje, finca, ingenio, sinRetraso.llegadaEstimadaMs + 10 * MINUTO);

    expect(durante.llegadaEstimadaMs).toBe(sinRetraso.llegadaEstimadaMs);
    expect(durante.situacion.estado).toBe("demora");
    expect(calcularViaje(viaje, finca, ingenio, sinRetraso.llegadaEstimadaMs + 20 * MINUTO).situacion.estado).toBe("en-playa");
  });
});

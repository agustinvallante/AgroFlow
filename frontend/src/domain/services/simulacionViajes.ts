/**
 * Simulación de viajes finca → ingenio sin GPS: la posición de cada camión se
 * deduce del reloj, la distancia a su finca y una velocidad estimada. Son
 * funciones puras; la fuente de los datos (la cola de turnos y la carga de la demo) queda afuera.
 */

import type { EstadoTurno } from "../entities/Turno";

/**
 * Rangos admitidos para un viaje. Fuera de ellos las horas dejan de ser
 * representables (`Date` lanza error) o el escenario deja de ser verosímil.
 */
export const LIMITES_VIAJE = {
  velocidadMinKmh: 1,
  velocidadMaxKmh: 120,
  /** Un día: tope de salida relativa y de retraso. */
  minutosMax: 1440,
} as const;

/** Instantes muy lejanos que todavía permiten calcular y mostrar una hora. */
const INSTANTE_MAXIMO_MS = 1e15;

const enRango = (valor: unknown, min: number, max: number): valor is number =>
  typeof valor === "number" && Number.isFinite(valor) && valor >= min && valor <= max;

export function esUbicacionValida(lugar: Partial<PuntoGeo>): lugar is PuntoGeo {
  return enRango(lugar.lat, -90, 90) && enRango(lugar.lon, -180, 180);
}

export const esVelocidadValida = (kmh: unknown): kmh is number =>
  enRango(kmh, LIMITES_VIAJE.velocidadMinKmh, LIMITES_VIAJE.velocidadMaxKmh);

export const esMinutosValidos = (minutos: unknown): minutos is number =>
  enRango(minutos, 0, LIMITES_VIAJE.minutosMax);

export const esInstanteValido = (ms: unknown): ms is number =>
  enRango(ms, -INSTANTE_MAXIMO_MS, INSTANTE_MAXIMO_MS);

/** Para datos de origen no confiable, como lo guardado en el navegador. */
export function esViajeValido(viaje: Partial<ViajeSimulado>): viaje is ViajeSimulado {
  return (
    typeof viaje.id === "string" &&
    typeof viaje.patente === "string" &&
    viaje.patente !== "" &&
    typeof viaje.fincaId === "string" &&
    esInstanteValido(viaje.salidaMs) &&
    esVelocidadValida(viaje.velocidadKmh) &&
    esMinutosValidos(viaje.retrasoMin)
  );
}

export interface PuntoGeo {
  lat: number;
  lon: number;
}

export interface FincaMapa extends PuntoGeo {
  id: string;
  nombre: string;
}

export interface ViajeSimulado {
  id: string;
  patente: string;
  fincaId: string;
  salidaMs: number;
  velocidadKmh: number;
  /** Minutos que el viaje tarda de más; reemplaza a la llegada confirmada que informará la API. */
  retrasoMin: number;
}

export type EstadoViaje = "programado" | "en-ruta" | "demora" | "en-playa";

export interface SituacionViaje {
  estado: EstadoViaje;
  /** Fracción recorrida de la ruta, entre 0 y 1. */
  progreso: number;
}

export interface ViajeCalculado {
  viaje: ViajeSimulado;
  finca: FincaMapa;
  distanciaKm: number;
  llegadaEstimadaMs: number;
  llegadaMs: number;
  situacion: SituacionViaje;
  /** Presente si el viaje sale de un turno de la cola; si no, es un viaje de la demo. */
  turno?: TurnoDelViaje;
}

export interface TurnoDelViaje {
  id: string;
  ventana: { inicio: string; fin: string };
  /** Nombre del transportista que informa la API. */
  transportista: string;
  /** Estado que informa la API; el del mapa es una estimación a partir de él. */
  estado: EstadoTurno;
}

/** La ruta real es más larga que la línea recta entre finca e ingenio. Supuesto a calibrar. */
export const FACTOR_RUTA = 1.3;

/** Sin llegada confirmada no se afirma que el camión entró: espera cerca del ingenio. */
export const PROGRESO_MAXIMO_SIN_CONFIRMAR = 0.95;

const RADIO_TIERRA_KM = 6371;
const MS_POR_MINUTO = 60_000;
const MS_POR_HORA = 3_600_000;

const radianes = (grados: number) => (grados * Math.PI) / 180;

/** Distancia estimada por ruta: haversine por {@link FACTOR_RUTA}. */
export function distanciaRutaKm(a: PuntoGeo, b: PuntoGeo): number {
  const dLat = radianes(b.lat - a.lat);
  const dLon = radianes(b.lon - a.lon);
  const h =
    Math.sin(dLat / 2) ** 2 + Math.cos(radianes(a.lat)) * Math.cos(radianes(b.lat)) * Math.sin(dLon / 2) ** 2;
  return 2 * RADIO_TIERRA_KM * Math.asin(Math.sqrt(h)) * FACTOR_RUTA;
}

export function duracionEstimadaMs(distanciaKm: number, velocidadKmh: number): number {
  return (distanciaKm / velocidadKmh) * MS_POR_HORA;
}

export function calcularSituacion(
  salidaMs: number,
  duracionMs: number,
  llegadaMs: number | null,
  ahoraMs: number,
): SituacionViaje {
  if (ahoraMs < salidaMs) return { estado: "programado", progreso: 0 };
  if (llegadaMs !== null && ahoraMs >= llegadaMs) return { estado: "en-playa", progreso: 1 };
  const avance = duracionMs > 0 ? (ahoraMs - salidaMs) / duracionMs : 1;
  if (avance >= 1) return { estado: "demora", progreso: PROGRESO_MAXIMO_SIN_CONFIRMAR };
  const llegaEnHora = llegadaMs !== null && llegadaMs <= salidaMs + duracionMs;
  return { estado: "en-ruta", progreso: avance * (llegaEnHora ? 1 : PROGRESO_MAXIMO_SIN_CONFIRMAR) };
}

export function calcularViaje(
  viaje: ViajeSimulado,
  finca: FincaMapa,
  ingenio: PuntoGeo,
  ahoraMs: number,
): ViajeCalculado {
  const distanciaKm = distanciaRutaKm(finca, ingenio);
  const duracionMs = duracionEstimadaMs(distanciaKm, viaje.velocidadKmh);
  const llegadaEstimadaMs = viaje.salidaMs + duracionMs;
  const llegadaMs = llegadaEstimadaMs + viaje.retrasoMin * MS_POR_MINUTO;
  return {
    viaje,
    finca,
    distanciaKm,
    llegadaEstimadaMs,
    llegadaMs,
    situacion: calcularSituacion(viaje.salidaMs, duracionMs, llegadaMs, ahoraMs),
  };
}

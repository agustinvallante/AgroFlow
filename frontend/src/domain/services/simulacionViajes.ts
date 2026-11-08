/**
 * Simulación de viajes finca → ingenio sin GPS: la posición de cada camión se
 * deduce del reloj, la distancia a su finca y una velocidad estimada. Son
 * funciones puras; la fuente de los datos (hoy de ejemplo) queda afuera.
 */

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

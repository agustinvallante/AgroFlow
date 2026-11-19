/**
 * Viajes de la cola de turnos. ZafraFlow API publica el estado de cada turno, la hora en que salió, sus problemas
 * en ruta y su ventana, pero no la posición: el mapa la estima a partir de esos eventos.
 *
 * - Asignado: el camión espera en su finca hasta que el transportista avise que salió.
 * - En camino: avanza desde la salida que informa la API (o, si no la informa, desde que el mapa lo vio en
 *   camino), a una velocidad fija y sin contar el tiempo detenido por problemas en ruta.
 * - Con un problema abierto queda con demora donde se detuvo; al resolverse sigue desde ese punto y llega más tarde.
 * - Al llegar a la hora estimada espera en la puerta: figura llegado recién cuando la API informa que está en el
 *   ingenio (En espera, Ingresado o En descarga).
 *
 * Los instantes de la API son reales; `RelojDeTurno.aHoraMapa` los lleva al reloj del mapa, que con x10, x60 o +1 h
 * va adelantado. El estado del mapa es una estimación visual: no reemplaza ni recalcula el estado del turno, que
 * sigue siendo el de la API (ADR-003).
 */
import type { EstadoTurno, Turno } from "../entities/Turno";
import {
  distanciaRutaKm,
  duracionEstimadaMs,
  PROGRESO_MAXIMO_SIN_CONFIRMAR,
  type FincaMapa,
  type PuntoGeo,
  type SituacionViaje,
  type ViajeCalculado,
} from "./simulacionViajes";

/** Velocidad supuesta para los viajes de la cola, la misma que propone el formulario de la demo. */
export const VELOCIDAD_TURNOS_KMH = 40;

/** Estados de la API en los que el camión ya está en el ingenio. */
const EN_INGENIO: EstadoTurno[] = ["cancha", "ingresado", "descargando"];
/** Estados que ya no muestran un viaje. */
const TERMINADOS: EstadoTurno[] = ["completado", "cancelado"];

/** Lo que el mapa sabe de su propio reloj para ubicar un turno. */
export interface RelojDeTurno {
  /** Convierte un instante real de la API a la hora del mapa. */
  aHoraMapa: (realMs: number) => number;
  /** Hora del mapa en que vio el turno en camino por primera vez; se usa si la API no informa la salida. */
  salidaVistaMs?: number;
  /** Hora del mapa en que vio el problema abierto por primera vez. */
  detenidoVistoMs?: number;
}

const SIN_ADELANTO: RelojDeTurno = { aHoraMapa: (ms) => ms };

const normalizar = (texto: string) =>
  texto.trim().toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "");

/** La API informa la finca del turno por nombre: se busca en el catálogo de fincas. */
export function fincaDelTurno(turno: Turno, fincas: FincaMapa[]): FincaMapa | null {
  const nombre = normalizar(turno.finca);
  return fincas.find((f) => normalizar(f.nombre) === nombre) ?? null;
}

/** Instante de la API en la hora del mapa, o null si falta o no es una fecha. */
function enHoraMapa(rfc3339: string | undefined, reloj: RelojDeTurno): number | null {
  const ms = rfc3339 ? Date.parse(rfc3339) : NaN;
  return Number.isFinite(ms) ? reloj.aHoraMapa(ms) : null;
}

/** Tiempo detenido dentro del viaje: entre la salida y ahora. */
const detenidoEntre = (inicioMs: number, finMs: number, salidaMs: number, ahoraMs: number) =>
  Math.max(0, Math.min(finMs, ahoraMs) - Math.max(inicioMs, salidaMs));

/** Hora del mapa en que salió y tiempo que lleva detenido, contando un problema todavía abierto. */
function recorrido(turno: Turno, salidaMs: number, ahoraMs: number, reloj: RelojDeTurno) {
  let detenidoMs = 0;
  for (const parada of turno.paradas ?? []) {
    const inicioMs = enHoraMapa(parada.inicio, reloj);
    const finMs = enHoraMapa(parada.fin, reloj);
    if (inicioMs !== null && finMs !== null) detenidoMs += detenidoEntre(inicioMs, finMs, salidaMs, ahoraMs);
  }
  if (turno.incidente) {
    // Si el mapa lo vio antes de la hora convertida del reporte, se detiene donde lo vio: nunca retrocede.
    const reportadoMs = enHoraMapa(turno.incidente.reportadoEn, reloj) ?? ahoraMs;
    const desdeMs = Math.min(reloj.detenidoVistoMs ?? reportadoMs, reportadoMs);
    detenidoMs += detenidoEntre(desdeMs, ahoraMs, salidaMs, ahoraMs);
  }
  return detenidoMs;
}

function situacionEnCamino(turno: Turno, avance: number): SituacionViaje {
  // Sin llegada confirmada por la API el camión no entra: el recorrido termina en la puerta del ingenio.
  const progreso = Math.min(Math.max(avance, 0), 1) * PROGRESO_MAXIMO_SIN_CONFIRMAR;
  if (turno.incidente) return { estado: "demora", progreso };
  return avance >= 1 ? { estado: "en-puerta", progreso } : { estado: "en-ruta", progreso };
}

export function calcularViajeDeTurno(
  turno: Turno,
  finca: FincaMapa,
  ingenio: PuntoGeo,
  ahoraMs: number,
  reloj: RelojDeTurno = SIN_ADELANTO,
): ViajeCalculado | null {
  if (TERMINADOS.includes(turno.estado)) return null;
  const inicioVentanaMs = Date.parse(turno.ventana.inicio);
  if (!Number.isFinite(inicioVentanaMs)) return null;

  const distanciaKm = distanciaRutaKm(finca, ingenio);
  const duracionMs = duracionEstimadaMs(distanciaKm, VELOCIDAD_TURNOS_KMH);
  const enIngenio = EN_INGENIO.includes(turno.estado);
  const salidaInformadaMs = enHoraMapa(turno.salidaEn, reloj);
  const salio = turno.estado === "viaje" || (enIngenio && salidaInformadaMs !== null);

  // Sin salida, la referencia es la ventana: llegar a su inicio.
  const salidaMs = salio ? (salidaInformadaMs ?? reloj.salidaVistaMs ?? ahoraMs) : inicioVentanaMs - duracionMs;
  const detenidoMs = salio ? recorrido(turno, salidaMs, ahoraMs, reloj) : 0;
  const llegadaEstimadaMs = salidaMs + duracionMs + detenidoMs;
  const avance = duracionMs > 0 ? (ahoraMs - salidaMs - detenidoMs) / duracionMs : 1;

  const situacion: SituacionViaje = enIngenio
    ? { estado: "en-playa", progreso: 1 }
    : turno.estado === "viaje"
      ? situacionEnCamino(turno, avance)
      : { estado: "programado", progreso: 0 };

  return {
    viaje: {
      id: `turno-${turno.id}`,
      patente: turno.patente,
      fincaId: finca.id,
      salidaMs,
      velocidadKmh: VELOCIDAD_TURNOS_KMH,
      retrasoMin: 0,
    },
    finca,
    distanciaKm,
    llegadaEstimadaMs,
    // La API no publica la hora real de llegada: se usa la estimada.
    llegadaMs: llegadaEstimadaMs,
    situacion,
    turno: {
      id: turno.id,
      ventana: turno.ventana,
      estado: turno.estado,
      transportista: turno.chofer,
      ...(turno.incidente ? { incidente: turno.incidente } : {}),
    },
  };
}

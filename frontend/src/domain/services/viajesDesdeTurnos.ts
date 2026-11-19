/**
 * MEDIDA TEMPORAL. ZafraFlow API publica los turnos de la cola (patente,
 * finca, ventana y estado) pero no los viajes: ni hora de salida, ni posición,
 * ni hora de llegada. Hasta que lo haga, el mapa deriva un viaje por turno:
 *
 * - Se supone que el camión debe llegar al inicio de su ventana, y la salida
 *   se calcula hacia atrás con la distancia estimada y una velocidad fija.
 * - Si la API informa que el turno ya está en el ingenio (En espera,
 *   Ingresado o En descarga), el viaje figura como llegado.
 * - Si no, la posición se estima con el reloj y el camión llega a la hora
 *   estimada. Para la demo importa mostrar el recorrido, no la precisión: con
 *   x10 o x60 los turnos avanzan como cualquier otro viaje.
 * - Si el turno tiene un problema en ruta abierto, figura con demora y queda
 *   donde estaba al reportarlo hasta que la API informe que se resolvió.
 *
 * El estado del mapa es una estimación visual: no reemplaza ni recalcula el
 * estado del turno, que sigue siendo el de la API (ADR-003).
 */
import type { EstadoTurno, IncidenteEnRuta, Turno } from "../entities/Turno";
import {
  calcularSituacion,
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

const normalizar = (texto: string) =>
  texto.trim().toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "");

/** La API informa la finca del turno por nombre: se busca en el catálogo de fincas. */
export function fincaDelTurno(turno: Turno, fincas: FincaMapa[]): FincaMapa | null {
  const nombre = normalizar(turno.finca);
  return fincas.find((f) => normalizar(f.nombre) === nombre) ?? null;
}

/** El camión se detuvo al reportar el problema: no avanza más allá de donde estaba, ni llega sin confirmación. */
function situacionConIncidente(
  incidente: IncidenteEnRuta,
  salidaMs: number,
  duracionMs: number,
  llegadaEstimadaMs: number,
  ahoraMs: number,
): SituacionViaje {
  const reportadoMs = Date.parse(incidente.reportadoEn);
  const hastaMs = Number.isFinite(reportadoMs) ? Math.min(reportadoMs, ahoraMs) : ahoraMs;
  const { progreso } = calcularSituacion(salidaMs, duracionMs, llegadaEstimadaMs, hastaMs);
  return { estado: "demora", progreso: Math.min(progreso, PROGRESO_MAXIMO_SIN_CONFIRMAR) };
}

export function calcularViajeDeTurno(
  turno: Turno,
  finca: FincaMapa,
  ingenio: PuntoGeo,
  ahoraMs: number,
): ViajeCalculado | null {
  if (TERMINADOS.includes(turno.estado)) return null;
  const llegadaEstimadaMs = Date.parse(turno.ventana.inicio);
  if (!Number.isFinite(llegadaEstimadaMs)) return null;

  const distanciaKm = distanciaRutaKm(finca, ingenio);
  const duracionMs = duracionEstimadaMs(distanciaKm, VELOCIDAD_TURNOS_KMH);
  const salidaMs = llegadaEstimadaMs - duracionMs;
  const enIngenio = EN_INGENIO.includes(turno.estado);

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
    situacion: enIngenio
      ? { estado: "en-playa", progreso: 1 }
      : turno.incidente
        ? situacionConIncidente(turno.incidente, salidaMs, duracionMs, llegadaEstimadaMs, ahoraMs)
        : calcularSituacion(salidaMs, duracionMs, llegadaEstimadaMs, ahoraMs),
    turno: {
      id: turno.id,
      ventana: turno.ventana,
      estado: turno.estado,
      ...(turno.incidente ? { incidente: turno.incidente } : {}),
    },
  };
}

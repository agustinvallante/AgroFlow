import type { FiltrosTurno, NuevoTurnoManual, Turno } from "../entities/Turno";
import type { FrancoTimeline, MetricasPanel } from "../entities/MetricasPanel";

/**
 * Puerto (contrato) que define qué necesita la aplicación del mundo exterior
 * para trabajar con turnos. La capa de dominio/aplicación solo conoce esta
 * interfaz; quién la implementa (mock o HTTP contra AgroFlow API) es un
 * detalle de infraestructura.
 */
export interface TurnoRepository {
  obtenerTurnosDelDia(filtros?: FiltrosTurno): Promise<Turno[]>;
  obtenerTurno(id: string): Promise<Turno>;
  obtenerMetricasPanel(): Promise<MetricasPanel>;
  obtenerTimelineDelDia(): Promise<FrancoTimeline[]>;
  crearTurnoManual(datos: NuevoTurnoManual): Promise<Turno>;
  reasignarHorario(id: string, nuevaHora: string): Promise<Turno>;
  cancelarTurno(id: string): Promise<Turno>;
  avanzarEstado(id: string): Promise<Turno>;
  alternarEstadoMolienda(): Promise<MetricasPanel>;
}

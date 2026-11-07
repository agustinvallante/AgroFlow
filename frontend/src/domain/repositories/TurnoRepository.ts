import type { EstadoTurno, FiltrosTurno, NuevoTurnoManual, Turno } from "../entities/Turno";

/**
 * Puerto (contrato) que define qué necesita la aplicación del mundo exterior
 * para trabajar con turnos. La capa de dominio/aplicación solo conoce esta
 * interfaz; la implementación HTTP contra AgroFlow API es un detalle de
 * infraestructura. Solo incluye operaciones que existen en openapi.yaml.
 */
export interface TurnoRepository {
  obtenerTurnosDelDia(filtros?: FiltrosTurno): Promise<Turno[]>;
  obtenerTurno(id: string): Promise<Turno>;
  crearTurnoManual(datos: NuevoTurnoManual): Promise<Turno>;
  cancelarTurno(id: string): Promise<Turno>;
  /** Solicita el estado elegido por el operador; la fuente valida la transición. */
  cambiarEstado(id: string, nuevoEstado: EstadoTurno): Promise<Turno>;
}

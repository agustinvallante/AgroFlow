export type FlotaTipo = "propia" | "tercero";

export type EstadoTurno =
  | "pendiente"
  | "viaje"
  | "cancha"
  | "descargando"
  | "completado"
  | "demorado";

export type CanalAsignacion = "bot" | "manual";

export type PrioridadCorte = "alta" | "media" | "normal";

/**
 * Entidad de dominio: representa un turno de ingreso al ingenio
 * asignado a un camión/transportista. No conoce nada de la UI
 * ni de cómo se obtienen o persisten los datos.
 */
export interface Turno {
  id: string;
  hora: string; // "HH:MM"
  offsetMin: number; // minutos respecto al momento actual, usado para ordenar/timeline
  patente: string;
  chofer: string;
  finca: string;
  flota: FlotaTipo;
  horasDesdeCorte: number;
  estado: EstadoTurno;
  canal: CanalAsignacion;
  esperaMin: number;
}

export interface NuevoTurnoManual {
  patente: string;
  chofer: string;
  finca: string;
  flota: FlotaTipo;
  hora: string;
  horasDesdeCorte: number;
}

/** Regla de negocio pura: a partir de cuántas horas de corte se considera prioridad alta/media. */
export function calcularPrioridadCorte(horasDesdeCorte: number): PrioridadCorte {
  if (horasDesdeCorte >= 18) return "alta";
  if (horasDesdeCorte >= 8) return "media";
  return "normal";
}

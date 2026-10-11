export type FlotaTipo = "propia" | "tercero";

export type EstadoTurno =
  | "pendiente"
  | "viaje"
  | "cancha"
  | "ingresado"
  | "descargando"
  | "completado"
  | "demorado"
  | "cancelado";

export type CanalAsignacion = "bot" | "manual";

export type PrioridadCorte = "alta" | "media" | "normal";

/**
 * Entidad de dominio: representa un turno de ingreso al ingenio
 * asignado a un camión/transportista. No conoce nada de la UI
 * ni de cómo se obtienen o persisten los datos.
 *
 * Los campos que admiten `null` son datos que no toda fuente informa
 * (la API local de la demo no devuelve flota, canal, espera ni prioridad).
 * La UI los muestra como "—" en lugar de calcularlos.
 */
export interface Turno {
  id: string;
  hora: string; // "HH:MM" inicio de la ventana
  offsetMin: number; // minutos respecto al momento actual, usado para ordenar/timeline
  patente: string;
  chofer: string;
  finca: string;
  flota: FlotaTipo | null;
  horasDesdeCorte: number;
  estado: EstadoTurno;
  canal: CanalAsignacion | null;
  esperaMin: number | null;
  prioridad: PrioridadCorte | null;
  ventanaFin?: string; // "HH:MM"
  corteEn?: string; // ISO 8601
  cargaTon?: number;
  creadoEn?: string; // ISO 8601
}

/**
 * Alta manual con los identificadores naturales del seed. La ventana,
 * la prioridad y la capacidad las decide la API, no el formulario.
 */
export interface NuevoTurnoManual {
  telefono: string; // E.164, ej. +5493815550101
  patente: string;
  codigoFinca: string;
  corteEn: string; // ISO 8601 con desplazamiento horario
  cargaTon: number;
}

/** Filtros que la fuente de datos aplica del lado del servidor. */
export interface FiltrosTurno {
  fecha?: string; // YYYY-MM-DD
  estado?: EstadoTurno;
  patente?: string;
  telefono?: string;
}

/** Regla de negocio pura: a partir de cuántas horas de corte se considera prioridad alta/media. */
export function calcularPrioridadCorte(horasDesdeCorte: number): PrioridadCorte {
  if (horasDesdeCorte >= 18) return "alta";
  if (horasDesdeCorte >= 8) return "media";
  return "normal";
}

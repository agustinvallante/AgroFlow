/** Espejo de AppointmentStatus de openapi.yaml (ver appointmentMapper). */
export type EstadoTurno =
  | "pendiente"
  | "viaje"
  | "cancha"
  | "ingresado"
  | "descargando"
  | "completado"
  | "cancelado";

/**
 * Entidad de dominio: representa un turno de ingreso al ingenio
 * asignado a un camión/transportista. No conoce nada de la UI
 * ni de cómo se obtienen o persisten los datos.
 *
 * Solo contiene lo que informa AgroFlow API. Flota, canal, espera y
 * prioridad no están en el contrato y por eso no existen acá.
 */
export interface Turno {
  id: string;
  hora: string; // "HH:MM" inicio de la ventana
  offsetMin: number; // minutos respecto al momento actual, usado para ordenar/timeline
  patente: string;
  chofer: string;
  finca: string;
  horasDesdeCorte: number;
  estado: EstadoTurno;
  ventanaFin?: string; // "HH:MM"
  corteEn?: string; // ISO 8601
  cargaTon?: number;
  creadoEn?: string; // ISO 8601
}

/**
 * Alta manual con los identificadores naturales que ya existen en la API. La ventana,
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

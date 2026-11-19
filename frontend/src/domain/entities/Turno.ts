/** Espejo de AppointmentStatus de openapi.yaml (ver appointmentMapper). */
export type EstadoTurno =
  | "pendiente"
  | "viaje"
  | "cancha"
  | "ingresado"
  | "descargando"
  | "completado"
  | "cancelado";

/** Espejo de IncidentType de openapi.yaml (ver appointmentMapper). */
export type TipoIncidente = "pinchazo" | "falla-mecanica" | "accidente" | "otro";

/**
 * Problema que el transportista informó durante el viaje (pinchazo, falla,
 * accidente). Es información para el operador: no cambia el estado ni la
 * ventana del turno.
 */
export interface IncidenteEnRuta {
  id: string;
  tipo: TipoIncidente;
  puedeSeguir: boolean;
  demoraEstimadaMin: number;
  detalle: string | null;
  reportadoEn: string; // RFC 3339
}

/** Problema en ruta ya resuelto: el camión estuvo detenido entre ambos instantes (RFC 3339). */
export interface ParadaEnRuta {
  inicio: string;
  fin: string;
}

/**
 * Entidad de dominio: representa un turno de ingreso al ingenio
 * asignado a un camión/transportista. No conoce nada de la UI
 * ni de cómo se obtienen o persisten los datos.
 *
 * Solo contiene lo que informa ZafraFlow API. Flota, canal, espera y
 * prioridad no están en el contrato y por eso no existen acá.
 */
export interface Turno {
  id: string;
  /** RFC 3339 completos, con el desplazamiento del ingenio informado por la API. */
  ventana: { inicio: string; fin: string };
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
  /** Incidente abierto que informa la API; ausente si no hay ninguno. */
  incidente?: IncidenteEnRuta;
  /** Hora en que salió hacia el ingenio (RFC 3339); ausente si la API no la informa. */
  salidaEn?: string;
  /** Problemas en ruta ya resueltos, en orden; el abierto está en `incidente`. */
  paradas?: ParadaEnRuta[];
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

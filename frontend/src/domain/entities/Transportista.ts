import type { FlotaTipo } from "./Turno";

/**
 * La API de la demo publica nombre, DNI, WhatsApp y estado. Patente, finca,
 * flota y turnos del mes no forman parte de ese contrato: con fuente http
 * llegan como null y la vista los omite.
 */
export interface Transportista {
  id: string;
  nombre: string;
  dni?: string | null;
  patente: string | null;
  telefono: string;
  finca: string | null;
  flota: FlotaTipo | null;
  turnosEsteMes: number | null;
  activo: boolean;
  /** Informado por la API; undefined en la fuente mock. */
  puedeSolicitarTurnos?: boolean;
}

export interface NuevoTransportista {
  nombre: string;
  dni?: string;
  patente: string;
  /** WhatsApp en formato internacional (+549…): reconoce al remitente del canal. */
  telefono: string;
  finca: string;
  flota: FlotaTipo;
}

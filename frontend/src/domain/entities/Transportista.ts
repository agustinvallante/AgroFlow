/** Lo que publica el contrato de transportistas de AgroFlow API. */
export interface Transportista {
  id: string;
  nombre: string;
  dni: string;
  /** Patentes de los camiones asociados; null si no tiene ninguno. */
  patente: string | null;
  telefono: string;
  activo: boolean;
  puedeSolicitarTurnos: boolean;
}

export interface NuevoTransportista {
  nombre: string;
  dni: string;
  /** WhatsApp en formato internacional (+549…): reconoce al remitente del canal. */
  telefono: string;
}

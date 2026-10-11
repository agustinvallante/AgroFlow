import type { FlotaTipo } from "./Turno";

export interface Transportista {
  id: string;
  nombre: string;
  patente: string;
  telefono: string;
  finca: string;
  flota: FlotaTipo;
  turnosEsteMes: number;
  activo: boolean;
}

export interface NuevoTransportista {
  nombre: string;
  patente: string;
  telefono: string;
  finca: string;
  flota: FlotaTipo;
}

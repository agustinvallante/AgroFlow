/** Lectura de Window (OpenAPI): todos los cupos los informa el backend. */
export interface VentanaRecepcion {
  id: string;
  startAt: string;
  endAt: string;
  capacity: number;
  occupied: number;
  available: number;
}

import type { NuevoTransportista, Transportista } from "../entities/Transportista";

export interface TransportistaRepository {
  obtenerTodos(): Promise<Transportista[]>;
  crear(datos: NuevoTransportista): Promise<Transportista>;
  /** Corrige nombre, DNI o WhatsApp de un transportista activo. */
  actualizar(id: string, datos: NuevoTransportista): Promise<Transportista>;
  cambiarEstadoActivo(id: string, activo: boolean): Promise<Transportista>;
}

import type { NuevoTransportista, Transportista } from "../entities/Transportista";

export interface TransportistaRepository {
  obtenerTodos(): Promise<Transportista[]>;
  crear(datos: NuevoTransportista): Promise<Transportista>;
  cambiarEstadoActivo(id: string): Promise<Transportista>;
}

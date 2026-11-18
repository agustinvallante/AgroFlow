import type { Asociacion, Camion } from "../entities/Asociacion";

export interface AsociacionRepository {
  obtenerCamionesActivos(): Promise<Camion[]>;
  obtenerAsociacionesActivas(): Promise<Asociacion[]>;
  asociar(transportistaId: string, camionId: string): Promise<void>;
  desasociar(asociacionId: string): Promise<void>;
}

import type { Finca } from "../entities/Finca";

export interface FincaRepository {
  obtenerActivas(): Promise<Finca[]>;
}

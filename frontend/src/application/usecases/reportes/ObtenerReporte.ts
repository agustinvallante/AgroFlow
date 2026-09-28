import type { RangoReporte } from "../../../domain/entities/Reporte";
import type { ReporteRepository } from "../../../domain/repositories/ReporteRepository";

export class ObtenerReporte {
  private readonly repo: ReporteRepository;

  constructor(repo: ReporteRepository) {
    this.repo = repo;
  }
  execute(rango: RangoReporte) {
    return this.repo.obtenerReporte(rango);
  }
}

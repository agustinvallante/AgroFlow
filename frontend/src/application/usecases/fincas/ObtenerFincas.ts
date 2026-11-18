import type { Finca } from "../../../domain/entities/Finca";
import type { FincaRepository } from "../../../domain/repositories/FincaRepository";

export class ObtenerFincas {
  private readonly repo: FincaRepository;

  constructor(repo: FincaRepository) {
    this.repo = repo;
  }

  execute(): Promise<Finca[]> {
    return this.repo.obtenerActivas();
  }
}

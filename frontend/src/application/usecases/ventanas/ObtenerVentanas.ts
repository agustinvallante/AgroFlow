import type { VentanaRepository } from "../../../domain/repositories/VentanaRepository";

export class ObtenerVentanas {
  private readonly repo: VentanaRepository;

  constructor(repo: VentanaRepository) {
    this.repo = repo;
  }

  execute(fecha?: string) {
    return this.repo.obtenerPorFecha(fecha);
  }
}

import type {
  NuevoTransportista,
  Transportista,
} from "../../../domain/entities/Transportista";
import type { TransportistaRepository } from "../../../domain/repositories/TransportistaRepository";

export class ObtenerTransportistas {
  private readonly repo: TransportistaRepository;

  constructor(repo: TransportistaRepository) {
    this.repo = repo;
  }
  execute(): Promise<Transportista[]> {
    return this.repo.obtenerTodos();
  }
}

export class CrearTransportista {
  private readonly repo: TransportistaRepository;

  constructor(repo: TransportistaRepository) {
    this.repo = repo;
  }
  execute(datos: NuevoTransportista): Promise<Transportista> {
    if (!datos.nombre || !datos.patente || !datos.finca) {
      return Promise.reject(new Error("Completá al menos nombre, patente y finca."));
    }
    return this.repo.crear({ ...datos, patente: datos.patente.toUpperCase() });
  }
}

export class CambiarEstadoActivoTransportista {
  private readonly repo: TransportistaRepository;

  constructor(repo: TransportistaRepository) {
    this.repo = repo;
  }
  execute(id: string): Promise<Transportista> {
    return this.repo.cambiarEstadoActivo(id);
  }
}

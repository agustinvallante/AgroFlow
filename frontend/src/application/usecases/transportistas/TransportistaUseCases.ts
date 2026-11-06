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
    // El WhatsApp es lo que identifica al transportista en el canal.
    const telefono = datos.telefono.replace(/[\s().-]/g, "");
    if (!datos.nombre.trim() || !telefono) {
      return Promise.reject(new Error("Completá al menos nombre y teléfono (WhatsApp)."));
    }
    return this.repo.crear({ ...datos, telefono, patente: datos.patente.toUpperCase() });
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

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
    return normalizar(datos).then((d) => this.repo.crear(d));
  }
}

/** Mismas reglas que el alta; la API sólo admite editar transportistas activos. */
export class EditarTransportista {
  private readonly repo: TransportistaRepository;

  constructor(repo: TransportistaRepository) {
    this.repo = repo;
  }
  execute(id: string, datos: NuevoTransportista): Promise<Transportista> {
    return normalizar(datos).then((d) => this.repo.actualizar(id, d));
  }
}

function normalizar(datos: NuevoTransportista): Promise<NuevoTransportista> {
  // El WhatsApp es lo que identifica al transportista en el canal.
  const telefono = datos.telefono.replace(/[\s().-]/g, "");
  if (!datos.nombre.trim() || !telefono) {
    return Promise.reject(new Error("Completá al menos nombre y teléfono (WhatsApp)."));
  }
  return Promise.resolve({ ...datos, telefono });
}

export class CambiarEstadoActivoTransportista {
  private readonly repo: TransportistaRepository;

  constructor(repo: TransportistaRepository) {
    this.repo = repo;
  }
  execute(id: string, activo: boolean): Promise<Transportista> {
    return this.repo.cambiarEstadoActivo(id, activo);
  }
}

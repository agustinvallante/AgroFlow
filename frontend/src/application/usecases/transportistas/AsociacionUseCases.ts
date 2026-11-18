import type { Camion, CamionAsociado } from "../../../domain/entities/Asociacion";
import type { AsociacionRepository } from "../../../domain/repositories/AsociacionRepository";

export interface FlotaAsociada {
  /** Camiones activos de cada transportista, indexados por su id. */
  porTransportista: Record<string, CamionAsociado[]>;
  /** Camiones activos sin transportista activo: los únicos que se pueden asociar. */
  libres: Camion[];
}

export class ObtenerFlotaAsociada {
  private readonly repo: AsociacionRepository;

  constructor(repo: AsociacionRepository) {
    this.repo = repo;
  }

  async execute(): Promise<FlotaAsociada> {
    const [camiones, asociaciones] = await Promise.all([
      this.repo.obtenerCamionesActivos(),
      this.repo.obtenerAsociacionesActivas(),
    ]);
    const porId = new Map(camiones.map((c) => [c.id, c]));
    const porTransportista: Record<string, CamionAsociado[]> = {};
    const ocupados = new Set<string>();
    for (const a of asociaciones) {
      ocupados.add(a.camionId);
      const camion = porId.get(a.camionId);
      if (!camion) continue; // Camión inhabilitado: no habilita turnos ni se muestra.
      (porTransportista[a.transportistaId] ??= []).push({ asociacionId: a.id, camionId: camion.id, patente: camion.patente });
    }
    for (const lista of Object.values(porTransportista)) lista.sort((x, y) => x.patente.localeCompare(y.patente));
    return { porTransportista, libres: camiones.filter((c) => !ocupados.has(c.id)) };
  }
}

export class AsociarCamion {
  private readonly repo: AsociacionRepository;

  constructor(repo: AsociacionRepository) {
    this.repo = repo;
  }

  execute(transportistaId: string, camionId: string): Promise<void> {
    if (!camionId) return Promise.reject(new Error("Elegí un camión para asociar."));
    return this.repo.asociar(transportistaId, camionId);
  }
}

export class DesasociarCamion {
  private readonly repo: AsociacionRepository;

  constructor(repo: AsociacionRepository) {
    this.repo = repo;
  }

  execute(asociacionId: string): Promise<void> {
    return this.repo.desasociar(asociacionId);
  }
}

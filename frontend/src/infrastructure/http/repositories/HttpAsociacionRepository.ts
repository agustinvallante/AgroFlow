import type { Asociacion, Camion } from "../../../domain/entities/Asociacion";
import type { AsociacionRepository } from "../../../domain/repositories/AsociacionRepository";
import { ApiClient } from "../ApiClient";

interface TruckDto {
  id: string;
  plate: string;
  fleetType: string;
  isActive: boolean;
}

interface AssociationDto {
  id: string;
  driverId: string;
  truckId: string;
  isActive: boolean;
}

const ASSOCIATIONS = "/api/v1/associations";

/**
 * Asociaciones transportista-camión de AgroFlow API. Cada mutación lleva una
 * Idempotency-Key nueva; las reglas (un transportista activo por camión,
 * recursos activos) las valida el backend.
 */
export class HttpAsociacionRepository implements AsociacionRepository {
  private readonly api: ApiClient;
  private readonly nuevaClave: () => string;

  constructor(baseUrl: string, nuevaClave: () => string = () => crypto.randomUUID()) {
    this.api = new ApiClient(baseUrl);
    this.nuevaClave = nuevaClave;
  }

  async obtenerCamionesActivos(): Promise<Camion[]> {
    const camiones = await this.api.get<TruckDto[]>("/api/v1/trucks", { active: "true" });
    return camiones.map((c) => ({ id: c.id, patente: c.plate, tipoFlota: c.fleetType, activo: c.isActive }));
  }

  async obtenerAsociacionesActivas(): Promise<Asociacion[]> {
    const asociaciones = await this.api.get<AssociationDto[]>(ASSOCIATIONS, { active: "true" });
    return asociaciones.map((a) => ({ id: a.id, transportistaId: a.driverId, camionId: a.truckId, activa: a.isActive }));
  }

  async asociar(transportistaId: string, camionId: string): Promise<void> {
    await this.api.post(ASSOCIATIONS, { driverId: transportistaId, truckId: camionId }, {
      "Idempotency-Key": this.nuevaClave(),
    });
  }

  async desasociar(asociacionId: string): Promise<void> {
    await this.api.post(`${ASSOCIATIONS}/${encodeURIComponent(asociacionId)}/inactivation`, undefined, {
      "Idempotency-Key": this.nuevaClave(),
    });
  }
}

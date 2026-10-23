import type { NuevoTransportista, Transportista } from "../../../domain/entities/Transportista";
import type { TransportistaRepository } from "../../../domain/repositories/TransportistaRepository";
import { ApiClient } from "../ApiClient";
import type { DriverDto, MutationReceiptDto } from "../dto/DriverDto";
import { mapDriver, toDriverInput } from "../mappers/driverMapper";

const BASE = "/api/v1/drivers";

/**
 * Transportistas de AgroFlow API. Cada mutación lleva una Idempotency-Key
 * nueva; la API responde un recibo y el transportista se relee para mostrar
 * lo persistido.
 */
export class HttpTransportistaRepository implements TransportistaRepository {
  private readonly api: ApiClient;
  private readonly nuevaClave: () => string;

  constructor(baseUrl: string, nuevaClave: () => string = () => crypto.randomUUID()) {
    this.api = new ApiClient(baseUrl);
    this.nuevaClave = nuevaClave;
  }

  async obtenerTodos(): Promise<Transportista[]> {
    return (await this.api.get<DriverDto[]>(BASE)).map(mapDriver);
  }

  async crear(datos: NuevoTransportista): Promise<Transportista> {
    const recibo = await this.api.post<MutationReceiptDto>(BASE, toDriverInput(datos), {
      "Idempotency-Key": this.nuevaClave(),
    });
    return this.obtener(recibo.resourceId);
  }

  async cambiarEstadoActivo(id: string): Promise<Transportista> {
    const actual = await this.obtener(id);
    if (!actual.activo) {
      throw new Error("La reactivación de transportistas no está publicada en la API de la demo.");
    }
    await this.api.post<MutationReceiptDto>(`${BASE}/${encodeURIComponent(id)}/inactivation`, undefined, {
      "Idempotency-Key": this.nuevaClave(),
    });
    return this.obtener(id);
  }

  private async obtener(id: string): Promise<Transportista> {
    return mapDriver(await this.api.get<DriverDto>(`${BASE}/${encodeURIComponent(id)}`));
  }
}

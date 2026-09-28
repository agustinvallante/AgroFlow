import type { NuevoTransportista, Transportista } from "../../../domain/entities/Transportista";
import type { TransportistaRepository } from "../../../domain/repositories/TransportistaRepository";
import type { ApiClient } from "../ApiClient";
import type { DriverDto, DriverPageDto } from "../dto/backend.dto";
import { mapDriver, toFleetDto } from "../mappers/backend.mappers";

export class HttpTransportistaRepository implements TransportistaRepository {
  private readonly api: ApiClient;

  constructor(api: ApiClient) {
    this.api = api;
  }

  async obtenerTodos(): Promise<Transportista[]> {
    const page = await this.api.get<DriverPageDto>("/api/drivers");
    return page.items.map(mapDriver);
  }

  async crear(datos: NuevoTransportista): Promise<Transportista> {
    const creado = await this.api.post<DriverDto>("/api/drivers", {
      nombre: datos.nombre,
      patente: datos.patente,
      phone: datos.telefono,
      fleet: toFleetDto(datos.flota),
      fincaHabitual: datos.finca,
    });
    return mapDriver(creado);
  }

  async cambiarEstadoActivo(id: string): Promise<Transportista> {
    const actualizado = await this.api.patch<DriverDto>(`/api/drivers/${id}/toggle-active`);
    return mapDriver(actualizado);
  }
}

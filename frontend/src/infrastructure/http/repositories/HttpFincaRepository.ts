import type { Finca } from "../../../domain/entities/Finca";
import type { FincaRepository } from "../../../domain/repositories/FincaRepository";
import { ApiClient } from "../ApiClient";
import type { FarmDto } from "../dto/FarmDto";

export class HttpFincaRepository implements FincaRepository {
  private readonly api: ApiClient;

  constructor(baseUrl: string) {
    this.api = new ApiClient(baseUrl);
  }

  async obtenerActivas(): Promise<Finca[]> {
    const fincas = await this.api.get<FarmDto[]>("/api/v1/farms", { active: "true" });
    return fincas.map((f) => ({ id: f.id, codigo: f.code, nombre: f.name }));
  }
}

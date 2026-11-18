import type { VentanaRecepcion } from "../../../domain/entities/VentanaRecepcion";
import type { VentanaRepository } from "../../../domain/repositories/VentanaRepository";
import { ApiClient } from "../ApiClient";

export class HttpVentanaRepository implements VentanaRepository {
  private readonly api: ApiClient;

  constructor(baseUrl: string) {
    this.api = new ApiClient(baseUrl);
  }

  obtenerPorFecha(fecha?: string): Promise<VentanaRecepcion[]> {
    // El DTO Window ya coincide con la lectura de dominio. No derivar cupos
    // de appointments ni enviar la clave: la agrega el proxy de Vite.
    return this.api.get<VentanaRecepcion[]>("/api/v1/windows", { date: fecha });
  }
}

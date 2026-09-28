import type {
  NuevoTransportista,
  Transportista,
} from "../../../domain/entities/Transportista";
import type { TransportistaRepository } from "../../../domain/repositories/TransportistaRepository";
import { delay } from "../../../shared/utils/delay";
import { generarTransportistasSemilla, siguienteIdTransportista } from "../data/transportistas.mock";

export class MockTransportistaRepository implements TransportistaRepository {
  private transportistas: Transportista[] = generarTransportistasSemilla();

  async obtenerTodos(): Promise<Transportista[]> {
    return delay([...this.transportistas]);
  }

  async crear(datos: NuevoTransportista): Promise<Transportista> {
    const nuevo: Transportista = {
      id: siguienteIdTransportista(),
      nombre: datos.nombre,
      patente: datos.patente,
      telefono: datos.telefono || "—",
      finca: datos.finca,
      flota: datos.flota,
      turnosEsteMes: 0,
      activo: true,
    };
    this.transportistas.unshift(nuevo);
    return delay(nuevo);
  }

  async cambiarEstadoActivo(id: string): Promise<Transportista> {
    const t = this.transportistas.find((x) => x.id === id);
    if (!t) throw new Error("Transportista no encontrado");
    t.activo = !t.activo;
    return delay(t);
  }
}

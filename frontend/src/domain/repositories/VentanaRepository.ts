import type { VentanaRecepcion } from "../entities/VentanaRecepcion";

export interface VentanaRepository {
  /** YYYY-MM-DD; sin fecha, la API decide el día actual del ingenio. */
  obtenerPorFecha(fecha?: string): Promise<VentanaRecepcion[]>;
}

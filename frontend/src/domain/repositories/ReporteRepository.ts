import type { Reporte, RangoReporte } from "../entities/Reporte";

export interface ReporteRepository {
  obtenerReporte(rango: RangoReporte): Promise<Reporte>;
}

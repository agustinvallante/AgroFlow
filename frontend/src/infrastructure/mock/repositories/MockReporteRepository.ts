import type { RangoReporte, Reporte } from "../../../domain/entities/Reporte";
import type { ReporteRepository } from "../../../domain/repositories/ReporteRepository";
import { delay } from "../../../shared/utils/delay";
import { FINCAS } from "../data/base.mock";

const DIAS_SEMANA = ["Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom"];

export class MockReporteRepository implements ReporteRepository {
  async obtenerReporte(rango: RangoReporte): Promise<Reporte> {
    const factor = rango === "semana" ? 1 : rango === "mes" ? 4.3 : 26;

    const esperaPorDia = DIAS_SEMANA.map((dia) => ({
      dia,
      minutos: Math.floor(20 + Math.random() * 20),
    }));

    const pctPropia = Math.floor(45 + Math.random() * 20);

    const fincasConMayorEspera = [...FINCAS]
      .sort(() => Math.random() - 0.5)
      .slice(0, 5)
      .map((finca) => ({
        finca,
        turnos: Math.floor(30 + Math.random() * 120),
        esperaPromedioMin: Math.floor(20 + Math.random() * 30),
        tendenciaSube: Math.random() > 0.5,
      }));

    const reporte: Reporte = {
      kpi: {
        esperaPromedioMin: rango === "zafra" ? 24 : rango === "mes" ? 26 : 27,
        reduccionVsManualPct: rango === "zafra" ? 33 : 30,
        turnosTotales: Math.round(288 * factor),
        porcentajeGestionadoPorBot: rango === "zafra" ? 94 : 92,
      },
      esperaPorDia,
      distribucionFlota: {
        porcentajePropia: pctPropia,
        porcentajeTercero: 100 - pctPropia,
      },
      fincasConMayorEspera,
    };

    return delay(reporte, 300);
  }
}

export type RangoReporte = "semana" | "mes" | "zafra";

export interface KpiReporte {
  esperaPromedioMin: number;
  reduccionVsManualPct: number;
  turnosTotales: number;
  porcentajeGestionadoPorBot: number;
}

export interface EsperaPorDia {
  dia: string;
  minutos: number;
}

export interface DistribucionFlota {
  porcentajePropia: number;
  porcentajeTercero: number;
}

export interface FincaConEspera {
  finca: string;
  turnos: number;
  esperaPromedioMin: number;
  tendenciaSube: boolean;
}

export interface Reporte {
  kpi: KpiReporte;
  esperaPorDia: EsperaPorDia[];
  distribucionFlota: DistribucionFlota;
  fincasConMayorEspera: FincaConEspera[];
}

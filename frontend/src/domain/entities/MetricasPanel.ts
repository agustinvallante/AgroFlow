export type EstadoMolienda = "operando" | "detenida";

export interface MetricasPanel {
  camionesEnEsperaAhora: number;
  esperaPromedioMin: number;
  porcentajeGestionadoPorBot: number;
  estadoMolienda: EstadoMolienda;
  capacidadMoliendaTnH: number;
  diaDeZafra: number;
  totalDiasZafra: number;
}

export interface FrancoTimeline {
  hora: string; // "HH:MM"
  camionesPropia: number;
  camionesTercero: number;
  camionesDemorados: number;
  esActual: boolean;
}

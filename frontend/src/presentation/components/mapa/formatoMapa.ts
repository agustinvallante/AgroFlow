import { esInstanteValido, type EstadoViaje } from "../../../domain/services/simulacionViajes";
import { horaEnZonaIngenio } from "../../../shared/utils/date";

export const ESTADO_LABEL: Record<EstadoViaje, string> = {
  "en-ruta": "En ruta",
  demora: "Demora",
  programado: "Programado",
  "en-playa": "Llegó",
};

/** Un instante no representable muestra un guion en lugar de desmontar la vista. */
export const hora = (ms: number) => (esInstanteValido(ms) ? horaEnZonaIngenio(new Date(ms).toISOString()) : "--:--");
export const minutos = (ms: number) => Math.max(Math.round(ms / 60_000), 0);
export const km = (valor: number) => `${valor.toLocaleString("es-AR", { maximumFractionDigits: 1 })} km`;

import type { IncidenteEnRuta, TipoIncidente } from "../../../domain/entities/Turno";

export const TIPO_INCIDENTE_LABEL: Record<TipoIncidente, string> = {
  pinchazo: "Pinchazo",
  "falla-mecanica": "Falla mecánica",
  accidente: "Accidente",
  otro: "Otro",
};

/** Texto de la cola de turnos: "Pinchazo · Puede seguir · Demora estimada 30 min". */
export const textoIncidente = (i: IncidenteEnRuta) =>
  `${TIPO_INCIDENTE_LABEL[i.tipo]} · ${i.puedeSeguir ? "Puede seguir" : "No puede seguir"} · Demora estimada ${i.demoraEstimadaMin} min`;

/** Versión corta para el mapa: "Pinchazo · puede seguir · demora est. 30 min". */
export const textoIncidenteCorto = (i: IncidenteEnRuta) =>
  `${TIPO_INCIDENTE_LABEL[i.tipo]} · ${i.puedeSeguir ? "puede seguir" : "no puede seguir"} · demora est. ${i.demoraEstimadaMin} min`;

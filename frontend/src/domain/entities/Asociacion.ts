/** Camión del ingenio tal como lo publica `GET /api/v1/trucks`. */
export interface Camion {
  id: string;
  patente: string;
  /** `PROPIA` o `TERCEROS`. */
  tipoFlota: string;
  activo: boolean;
}

/**
 * Asociación explícita transportista-camión. Un camión tiene a lo sumo un
 * transportista activo; un transportista puede tener varios camiones.
 */
export interface Asociacion {
  id: string;
  transportistaId: string;
  camionId: string;
  activa: boolean;
}

/** Camiones activos asociados a un transportista, con la asociación que permite desasociarlos. */
export interface CamionAsociado {
  asociacionId: string;
  camionId: string;
  patente: string;
}

/** Esquemas de docs/contracts/openapi.yaml (etiqueta Transportistas). */

export interface DriverDto {
  id: string;
  name: string;
  dni: string;
  whatsApp: string;
  isActive: boolean;
  /** Camiones activos con asociación activa (sólo lectura). */
  activeTruckPlates: string[];
  /** Decidido por la API: activo y con asociación activa a un camión activo. */
  canRequestAppointments: boolean;
}

export interface DriverInputDto {
  name: string;
  dni: string;
  whatsApp: string;
}

export interface MutationReceiptDto {
  resourceId: string;
  resourceType: "driver";
  outcome: "created" | "updated" | "inactivated";
}

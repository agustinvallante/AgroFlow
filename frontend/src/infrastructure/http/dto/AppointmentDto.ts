/**
 * Tipos del JSON tal como lo define docs/contracts/openapi.yaml del
 * repositorio ZafraFlow (fuente de verdad). No se usan fuera de la capa
 * HTTP: los mappers los traducen a entidades del dominio.
 */

export type AppointmentStatusDto =
  | "ASIGNADO"
  | "EN_CAMINO"
  | "EN_ESPERA"
  | "INGRESADO"
  | "EN_DESCARGA"
  | "FINALIZADO"
  | "CANCELADO";

export interface EntityReferenceDto {
  id: string;
  name: string;
}

export interface TruckReferenceDto {
  id: string;
  plate: string;
}

export interface AppointmentWindowDto {
  startAt: string;
  endAt: string;
}

export type IncidentTypeDto = "PINCHAZO" | "FALLA_MECANICA" | "ACCIDENTE" | "OTRO";

/** Incidente en ruta abierto de un turno `EN_CAMINO`. */
export interface OpenIncidentDto {
  id: string;
  type: IncidentTypeDto;
  canContinue: boolean;
  estimatedDelayMinutes: number;
  description: string | null;
  reportedAt: string;
}

/** Problema en ruta resuelto: el camión estuvo detenido entre ambos instantes. */
export interface StopDto {
  startedAt: string;
  endedAt: string;
}

/** Elemento de GET /api/v1/appointments. */
export interface AppointmentSummaryDto {
  id: string;
  truck: TruckReferenceDto;
  window: AppointmentWindowDto;
  status: AppointmentStatusDto;
  /** Opcional: una API anterior a los incidentes no lo envía. */
  openIncident?: OpenIncidentDto | null;
  /** Opcional: una API anterior a la salida informada no lo envía. */
  departedAt?: string | null;
  stops?: StopDto[];
}

/** Respuesta de POST, GET /{id} y POST /{id}/transitions. */
export interface AppointmentDto {
  id: string;
  carrier: EntityReferenceDto;
  truck: TruckReferenceDto;
  farm: EntityReferenceDto;
  cutAt: string;
  estimatedLoadTons: number;
  window: AppointmentWindowDto;
  status: AppointmentStatusDto;
  createdAt: string;
  /** Opcional: una API anterior a los incidentes no lo envía. */
  openIncident?: OpenIncidentDto | null;
  /** Opcional: una API anterior a la salida informada no lo envía. */
  departedAt?: string | null;
  stops?: StopDto[];
}

export interface CreateAppointmentRequestDto {
  carrierPhone: string;
  truckPlate: string;
  farmCode: string;
  cutAt: string;
  estimatedLoadTons: number;
}

export interface TransitionAppointmentRequestDto {
  newStatus: AppointmentStatusDto;
}

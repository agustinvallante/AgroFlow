/**
 * Tipos del JSON tal como lo define docs/contracts/openapi.yaml del
 * repositorio AgroFlow (fuente de verdad). No se usan fuera de la capa
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

/** Elemento de GET /api/v1/appointments. */
export interface AppointmentSummaryDto {
  id: string;
  truck: TruckReferenceDto;
  window: AppointmentWindowDto;
  status: AppointmentStatusDto;
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

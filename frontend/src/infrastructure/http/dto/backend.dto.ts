/**
 * Estos tipos describen el JSON tal como sale del backend .NET, NO como lo
 * usa el dominio del frontend. Las diferencias son reales y deliberadas:
 *
 *   backend            frontend
 *   ---------------    ---------------
 *   fleet: "Own"       flota: "propia"
 *   status: "EnRoute"  estado: "viaje"
 *   channel:"WhatsApp" canal: "bot"
 *   windowStart (ISO)  hora: "14:30"
 *   fincaNombre        finca
 *   driverName         chofer
 *
 * Traducir entre ambos es trabajo de los mappers. Mantener estos DTOs
 * separados evita que un cambio en la API se filtre a toda la app.
 *
 * ASP.NET serializa las propiedades en camelCase y los enums como string
 * (por el JsonStringEnumConverter en Program.cs), así que los nombres de
 * abajo coinciden literalmente con lo que llega por la red.
 */

export type FleetTypeDto = "Own" | "Contractor";
export type ChannelTypeDto = "WhatsApp" | "Manual";
export type PositionSourceDto = "Estimated" | "Pin";

export type AppointmentStatusDto =
  | "Pending"
  | "EnRoute"
  | "InYard"
  | "Unloading"
  | "Completed"
  | "Delayed"
  | "Cancelled";

export interface IncidentDto {
  id: string;
  appointmentId: string;
  type: string;
  locationSource: string;
  lat: number | null;
  lng: number | null;
  reportedAt: string;
  estimatedDelayMin: number;
  resolvedAt: string | null;
  isResolved: boolean;
}

export interface AppointmentDto {
  id: string;
  patente: string;
  driverName: string;
  phone: string;
  fincaNombre: string;
  fleet: FleetTypeDto;
  channel: ChannelTypeDto;
  status: AppointmentStatusDto;
  cutTime: string;
  hoursSinceCut: number;
  priority: "alta" | "media" | "normal";
  priorityReason: string;
  windowStart: string;
  windowEnd: string;
  recommendedDeparture: string;
  eta: string;
  waitMinutes: number;
  positionSource: PositionSourceDto;
  incident: IncidentDto | null;
  simulated: boolean;
}

export interface AppointmentPageDto {
  items: AppointmentDto[];
  total: number;
}

export interface CreateAppointmentDto {
  patente: string;
  driverName: string;
  phone: string;
  fincaNombre: string;
  fleet: FleetTypeDto;
  cutTime: string;
  channel: ChannelTypeDto;
}

export interface DriverDto {
  id: string;
  nombre: string;
  patente: string;
  phone: string;
  fleet: FleetTypeDto;
  fincaHabitual: string | null;
  activo: boolean;
  turnosEsteMes: number;
}

export interface DriverPageDto {
  items: DriverDto[];
  total: number;
}

export interface TimelineSlotDto {
  hora: string;
  camionesPropia: number;
  camionesTercero: number;
  camionesDemorados: number;
  esActual: boolean;
}

export interface StatusDto {
  camionesEnEsperaAhora: number;
  avgWaitMinutes: number;
  botSharePct: number;
  molliendoOperando: boolean;
  millCapacityTnH: number;
  zafraDay: number;
  totalDiasZafra: number;
  timeline: TimelineSlotDto[];
  simulated: boolean;
}

export interface MillDto {
  operando: boolean;
  capacidadTnH: number;
  zafraDay: number;
  updatedAt: string;
  simulated: boolean;
}

export interface ConversationMessageDto {
  id: string;
  phone: string;
  direction: "Inbound" | "Outbound";
  text: string;
  kind: string;
  simulated: boolean;
  createdAt: string;
}

export interface ConversationSummaryDto {
  phone: string;
  driverName: string | null;
  lastMessageAt: string;
  lastMessagePreview: string;
  messages: ConversationMessageDto[];
}

export interface ConversationMetricsDto {
  mensajesHoy: number;
  turnosAsignadosPorBot: number;
  tiempoPromedioRespuestaSeg: number;
  conversacionesEscaladas: number;
}

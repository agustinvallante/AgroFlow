import type {
  EstadoTurno,
  FiltrosTurno,
  IncidenteEnRuta,
  NuevoTurnoManual,
  TipoIncidente,
  Turno,
} from "../../../domain/entities/Turno";
import type {
  AppointmentDto,
  AppointmentStatusDto,
  AppointmentSummaryDto,
  CreateAppointmentRequestDto,
  IncidentTypeDto,
  OpenIncidentDto,
} from "../dto/AppointmentDto";
import { horaDeRfc3339 } from "../../../shared/utils/date";

/** Tabla de docs/LOCAL_DEMO_INTEGRATION.md, con EN_ESPERA e INGRESADO separados. */
const ESTADO_MAP: Record<AppointmentStatusDto, EstadoTurno> = {
  ASIGNADO: "pendiente",
  EN_CAMINO: "viaje",
  EN_ESPERA: "cancha",
  INGRESADO: "ingresado",
  EN_DESCARGA: "descargando",
  FINALIZADO: "completado",
  CANCELADO: "cancelado",
};

const ESTADO_INVERSO: Partial<Record<EstadoTurno, AppointmentStatusDto>> = Object.fromEntries(
  Object.entries(ESTADO_MAP).map(([api, dominio]) => [dominio, api])
);

const TIPO_INCIDENTE_MAP: Record<IncidentTypeDto, TipoIncidente> = {
  PINCHAZO: "pinchazo",
  FALLA_MECANICA: "falla-mecanica",
  ACCIDENTE: "accidente",
  OTRO: "otro",
};

/** Sin incidente abierto devuelve undefined. Un tipo que este frontend no conoce se muestra como "otro". */
function mapOpenIncident(dto: OpenIncidentDto | null | undefined): IncidenteEnRuta | undefined {
  if (!dto) return undefined;
  return {
    id: dto.id,
    tipo: TIPO_INCIDENTE_MAP[dto.type] ?? "otro",
    puedeSeguir: dto.canContinue,
    demoraEstimadaMin: dto.estimatedDelayMinutes,
    detalle: dto.description ?? null,
    reportadoEn: dto.reportedAt,
  };
}

/** Estado del dominio → valor del contrato, o undefined si la API no lo tiene. */
export function toAppointmentStatus(estado: EstadoTurno): AppointmentStatusDto | undefined {
  return ESTADO_INVERSO[estado];
}

export function fromAppointmentStatus(status: AppointmentStatusDto): EstadoTurno {
  return ESTADO_MAP[status];
}

export function mapAppointment(dto: AppointmentDto): Turno {
  const incidente = mapOpenIncident(dto.openIncident);
  return {
    id: dto.id,
    ventana: { inicio: dto.window.startAt, fin: dto.window.endAt },
    // Hora escrita por la API, en la zona del ingenio (no la del navegador).
    hora: horaDeRfc3339(dto.window.startAt),
    ventanaFin: horaDeRfc3339(dto.window.endAt),
    // Solo para ordenar la cola; la ventana la asigna la API.
    offsetMin: Math.round((new Date(dto.window.startAt).getTime() - Date.now()) / 60000),
    patente: dto.truck.plate,
    chofer: dto.carrier.name,
    finca: dto.farm.name,
    horasDesdeCorte: Math.max(0, Math.round((Date.now() - new Date(dto.cutAt).getTime()) / 3600000)),
    estado: fromAppointmentStatus(dto.status),
    corteEn: dto.cutAt,
    cargaTon: dto.estimatedLoadTons,
    creadoEn: dto.createdAt,
    ...(incidente ? { incidente } : {}),
    ...(dto.departedAt ? { salidaEn: dto.departedAt } : {}),
    paradas: (dto.stops ?? []).map((p) => ({ inicio: p.startedAt, fin: p.endedAt })),
  };
}

/**
 * El listado trae solo id, camión, ventana y estado. El resto sale del
 * detalle (que no cambia salvo ventana y estado), así que ventana y estado
 * del resumen, que es la lectura más reciente, pisan los del detalle. El
 * incidente abierto, la salida y las paradas también, si el resumen los informa: así un incidente resuelto
 * desaparece en la siguiente lectura aunque el detalle cacheado lo tuviera.
 */
export function combinarResumenYDetalle(resumen: AppointmentSummaryDto, detalle: AppointmentDto): Turno {
  return mapAppointment({
    ...detalle,
    truck: resumen.truck,
    window: resumen.window,
    status: resumen.status,
    openIncident: resumen.openIncident !== undefined ? resumen.openIncident : detalle.openIncident,
    departedAt: resumen.departedAt !== undefined ? resumen.departedAt : detalle.departedAt,
    stops: resumen.stops ?? detalle.stops,
  });
}

export function toCreateAppointmentRequest(datos: NuevoTurnoManual): CreateAppointmentRequestDto {
  return {
    carrierPhone: datos.telefono,
    truckPlate: datos.patente,
    farmCode: datos.codigoFinca,
    cutAt: datos.corteEn,
    estimatedLoadTons: datos.cargaTon,
  };
}

/**
 * Traduce los filtros del dominio a query params. Devuelve null si algún
 * filtro no tiene equivalente en la API (el resultado sería vacío).
 */
export function toListQuery(filtros: FiltrosTurno = {}): Record<string, string | undefined> | null {
  let status: AppointmentStatusDto | undefined;
  if (filtros.estado) {
    status = ESTADO_INVERSO[filtros.estado];
    if (!status) return null;
  }
  return {
    date: filtros.fecha,
    status,
    truckPlate: filtros.patente,
    phone: filtros.telefono,
  };
}

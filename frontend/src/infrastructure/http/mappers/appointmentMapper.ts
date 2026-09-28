import type { EstadoTurno, FiltrosTurno, NuevoTurnoManual, Turno } from "../../../domain/entities/Turno";
import type {
  AppointmentDto,
  AppointmentStatusDto,
  AppointmentSummaryDto,
  CreateAppointmentRequestDto,
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

// "demorado" no existe en la API: no tiene equivalente.
const ESTADO_INVERSO: Partial<Record<EstadoTurno, AppointmentStatusDto>> = Object.fromEntries(
  Object.entries(ESTADO_MAP).map(([api, dominio]) => [dominio, api])
);


/** Estado del dominio → valor del contrato, o undefined si la API no lo tiene. */
export function toAppointmentStatus(estado: EstadoTurno): AppointmentStatusDto | undefined {
  return ESTADO_INVERSO[estado];
}

export function mapAppointment(dto: AppointmentDto): Turno {
  return {
    id: dto.id,
    // Hora escrita por la API, en la zona del ingenio (no la del navegador).
    hora: horaDeRfc3339(dto.window.startAt),
    ventanaFin: horaDeRfc3339(dto.window.endAt),
    // Solo para ordenar la cola; la ventana la asigna la API.
    offsetMin: Math.round((new Date(dto.window.startAt).getTime() - Date.now()) / 60000),
    patente: dto.truck.plate,
    chofer: dto.carrier.name,
    finca: dto.farm.name,
    horasDesdeCorte: Math.max(0, Math.round((Date.now() - new Date(dto.cutAt).getTime()) / 3600000)),
    estado: ESTADO_MAP[dto.status],
    corteEn: dto.cutAt,
    cargaTon: dto.estimatedLoadTons,
    creadoEn: dto.createdAt,
    // La API de la demo no informa estos datos y el dashboard no los calcula.
    flota: null,
    canal: null,
    esperaMin: null,
    prioridad: null,
  };
}

/**
 * El listado trae solo id, camión, ventana y estado. El resto sale del
 * detalle (que no cambia salvo ventana y estado), así que ventana y estado
 * del resumen, que es la lectura más reciente, pisan los del detalle.
 */
export function combinarResumenYDetalle(resumen: AppointmentSummaryDto, detalle: AppointmentDto): Turno {
  return mapAppointment({ ...detalle, truck: resumen.truck, window: resumen.window, status: resumen.status });
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

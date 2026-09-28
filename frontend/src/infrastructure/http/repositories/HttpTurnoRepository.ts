import type { EstadoTurno, FiltrosTurno, NuevoTurnoManual, Turno } from "../../../domain/entities/Turno";
import type { FrancoTimeline, MetricasPanel } from "../../../domain/entities/MetricasPanel";
import type { TurnoRepository } from "../../../domain/repositories/TurnoRepository";
import { ApiClient } from "../ApiClient";
import type {
  AppointmentDto,
  AppointmentStatusDto,
  AppointmentSummaryDto,
  TransitionAppointmentRequestDto,
} from "../dto/AppointmentDto";
import {
  combinarResumenYDetalle,
  mapAppointment,
  toAppointmentStatus,
  toCreateAppointmentRequest,
  toListQuery,
} from "../mappers/appointmentMapper";

const BASE = "/api/v1/appointments";

/**
 * Implementa TurnoRepository contra AgroFlow API (docs/contracts/openapi.yaml).
 * Casos de uso, hooks y vistas no saben si hablan con esto o con el mock.
 */
export class HttpTurnoRepository implements TurnoRepository {
  private readonly api: ApiClient;
  /**
   * Transportista, finca, corte y carga no cambian después del alta, así que
   * el detalle se pide una sola vez por turno y el polling solo trae el listado.
   */
  private readonly detalles = new Map<string, AppointmentDto>();

  constructor(baseUrl: string) {
    this.api = new ApiClient(baseUrl);
  }

  async obtenerTurnosDelDia(filtros?: FiltrosTurno): Promise<Turno[]> {
    const query = toListQuery(filtros);
    if (!query) return [];

    const resumenes = await this.api.get<AppointmentSummaryDto[]>(BASE, query);
    const turnos = await Promise.all(
      resumenes.map(async (resumen) => {
        const detalle = this.detalles.get(resumen.id) ?? (await this.pedirDetalle(resumen.id));
        return combinarResumenYDetalle(resumen, detalle);
      })
    );
    return turnos;
  }

  async obtenerTurno(id: string): Promise<Turno> {
    return mapAppointment(await this.pedirDetalle(id));
  }

  async crearTurnoManual(datos: NuevoTurnoManual): Promise<Turno> {
    const creado = await this.api.post<AppointmentDto>(BASE, toCreateAppointmentRequest(datos));
    this.detalles.set(creado.id, creado);
    return mapAppointment(creado);
  }

  async cambiarEstado(id: string, nuevoEstado: EstadoTurno): Promise<Turno> {
    // El destino lo elige el operador; la API decide si la transición es
    // válida y, si no, responde 409 INVALID_TRANSITION.
    const status = toAppointmentStatus(nuevoEstado);
    if (!status) throw new Error("La API no admite ese estado.");
    return this.transicionar(id, status);
  }

  async cancelarTurno(id: string): Promise<Turno> {
    return this.transicionar(id, "CANCELADO");
  }

  async reasignarHorario(): Promise<Turno> {
    throw new Error("La reasignación de horario no forma parte de la demo local.");
  }

  // Métricas, timeline y molienda no existen en la API de la demo. El
  // container conecta el panel a datos mock identificados como tales.
  async obtenerMetricasPanel(): Promise<MetricasPanel> {
    throw new Error("La API local no expone métricas del panel.");
  }

  async obtenerTimelineDelDia(): Promise<FrancoTimeline[]> {
    throw new Error("La API local no expone el timeline del día.");
  }

  async alternarEstadoMolienda(): Promise<MetricasPanel> {
    throw new Error("La API local no expone el estado de la molienda.");
  }

  private async pedirDetalle(id: string): Promise<AppointmentDto> {
    const detalle = await this.api.get<AppointmentDto>(`${BASE}/${encodeURIComponent(id)}`);
    this.detalles.set(id, detalle);
    return detalle;
  }

  private async transicionar(id: string, newStatus: AppointmentStatusDto): Promise<Turno> {
    const body: TransitionAppointmentRequestDto = { newStatus };
    const actualizado = await this.api.post<AppointmentDto>(
      `${BASE}/${encodeURIComponent(id)}/transitions`,
      body
    );
    this.detalles.set(id, actualizado);
    return mapAppointment(actualizado);
  }
}

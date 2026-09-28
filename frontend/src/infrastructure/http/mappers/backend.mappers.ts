import type { FlotaTipo } from "../../../domain/entities/Turno";
import type { Transportista } from "../../../domain/entities/Transportista";
import type { FrancoTimeline, MetricasPanel } from "../../../domain/entities/MetricasPanel";
import type { Conversacion, MensajeChat, MetricasChatbot } from "../../../domain/entities/Conversacion";
import type {
  ConversationMetricsDto,
  ConversationSummaryDto,
  DriverDto,
  FleetTypeDto,
  StatusDto,
} from "../dto/backend.dto";

/* ------------------------------------------------------------------ */
/* Tablas de traducción                                                */
/* ------------------------------------------------------------------ */

const FLOTA_MAP: Record<FleetTypeDto, FlotaTipo> = {
  Own: "propia",
  Contractor: "tercero",
};

const FLOTA_INVERSO: Record<FlotaTipo, FleetTypeDto> = {
  propia: "Own",
  tercero: "Contractor",
};

export const toFleetDto = (flota: FlotaTipo): FleetTypeDto => FLOTA_INVERSO[flota];

/* ------------------------------------------------------------------ */
/* Utilidades                                                          */
/* ------------------------------------------------------------------ */

/** "2026-09-16T14:30:00" → "14:30" (lo que muestra la tabla). */
function horaDeIso(iso: string): string {
  const d = new Date(iso);
  return `${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
}

/* ------------------------------------------------------------------ */
/* Transportistas                                                      */
/* ------------------------------------------------------------------ */

export function mapDriver(dto: DriverDto): Transportista {
  return {
    id: dto.id,
    nombre: dto.nombre,
    patente: dto.patente,
    telefono: dto.phone,
    finca: dto.fincaHabitual ?? "—",
    flota: FLOTA_MAP[dto.fleet],
    turnosEsteMes: dto.turnosEsteMes,
    activo: dto.activo,
  };
}

/* ------------------------------------------------------------------ */
/* Panel general                                                       */
/* ------------------------------------------------------------------ */

export function mapStatus(dto: StatusDto): MetricasPanel {
  return {
    camionesEnEsperaAhora: dto.camionesEnEsperaAhora,
    esperaPromedioMin: Math.round(dto.avgWaitMinutes),
    porcentajeGestionadoPorBot: Math.round(dto.botSharePct),
    estadoMolienda: dto.molliendoOperando ? "operando" : "detenida",
    capacidadMoliendaTnH: dto.millCapacityTnH,
    diaDeZafra: dto.zafraDay,
    totalDiasZafra: dto.totalDiasZafra,
  };
}

export function mapTimeline(dto: StatusDto): FrancoTimeline[] {
  return dto.timeline.map((slot) => ({
    hora: slot.hora,
    camionesPropia: slot.camionesPropia,
    camionesTercero: slot.camionesTercero,
    camionesDemorados: slot.camionesDemorados,
    esActual: slot.esActual,
  }));
}

/* ------------------------------------------------------------------ */
/* Conversaciones                                                      */
/* ------------------------------------------------------------------ */

export function mapConversacion(dto: ConversationSummaryDto): Conversacion {
  const mensajes: MensajeChat[] = dto.messages.map((m) => ({
    autor: m.direction === "Inbound" ? "transportista" : "bot",
    texto: m.text,
  }));

  // El backend no marca conversaciones como "escaladas" todavía; se asume
  // resuelta si el último mensaje lo mandó el bot.
  const ultimo = dto.messages[dto.messages.length - 1];
  const estado = ultimo?.direction === "Outbound" ? "bot" : "resuelto";

  return {
    id: dto.phone,
    nombre: dto.driverName ? `${dto.driverName} · ${dto.phone}` : dto.phone,
    hora: horaDeIso(dto.lastMessageAt),
    preview: dto.lastMessagePreview,
    estado,
    mensajes,
  };
}

export function mapMetricasChatbot(dto: ConversationMetricsDto): MetricasChatbot {
  return {
    mensajesHoy: dto.mensajesHoy,
    turnosAsignadosPorBot: dto.turnosAsignadosPorBot,
    tiempoPromedioRespuestaSeg: dto.tiempoPromedioRespuestaSeg,
    conversacionesEscaladas: dto.conversacionesEscaladas,
  };
}

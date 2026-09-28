import {
  calcularPrioridadCorte,
  type EstadoTurno,
  type FiltrosTurno,
  type NuevoTurnoManual,
  type Turno,
} from "../../../domain/entities/Turno";
import type { FrancoTimeline, MetricasPanel } from "../../../domain/entities/MetricasPanel";
import type { TurnoRepository } from "../../../domain/repositories/TurnoRepository";
import { delay } from "../../../shared/utils/delay";
import { horaConOffset } from "../../../shared/utils/date";
import { generarTurnosSemilla, siguienteIdTurno } from "../data/turnos.mock";

/**
 * Implementación "en memoria" del puerto TurnoRepository.
 * Simula lo que hoy hace n8n + un backend real, para poder
 * desarrollar y demostrar el frontend sin conexión.
 *
 * Es el respaldo de VITE_DATA_SOURCE=mock; la implementación contra la
 * API real es HttpTurnoRepository.
 */
const SIGUIENTE_ESTADO: Partial<Record<EstadoTurno, EstadoTurno>> = {
  pendiente: "viaje",
  viaje: "cancha",
  cancha: "ingresado",
  ingresado: "descargando",
  descargando: "completado",
};

export class MockTurnoRepository implements TurnoRepository {
  private turnos: Turno[] = generarTurnosSemilla();
  private moliendaOperando = true;

  async obtenerTurnosDelDia(filtros: FiltrosTurno = {}): Promise<Turno[]> {
    const patente = filtros.patente?.replace(/[^A-Za-z0-9]/g, "").toUpperCase();
    return delay(
      this.turnos.filter(
        (t) =>
          (!filtros.estado || t.estado === filtros.estado) &&
          (!patente || t.patente.replace(/[^A-Za-z0-9]/g, "").toUpperCase() === patente)
      )
    );
  }

  async obtenerTurno(id: string): Promise<Turno> {
    return delay({ ...this.buscar(id) });
  }

  async obtenerMetricasPanel(): Promise<MetricasPanel> {
    const activos = this.turnos.filter((t) =>
      ["viaje", "cancha", "ingresado", "descargando"].includes(t.estado)
    );
    const gestionadosPorBot = this.turnos.filter((t) => t.canal === "bot").length;
    const metricas: MetricasPanel = {
      camionesEnEsperaAhora: activos.length,
      esperaPromedioMin: 27,
      porcentajeGestionadoPorBot: Math.round((gestionadosPorBot / (this.turnos.length || 1)) * 100),
      estadoMolienda: this.moliendaOperando ? "operando" : "detenida",
      capacidadMoliendaTnH: 340,
      diaDeZafra: 42,
      totalDiasZafra: 180,
    };
    return delay(metricas);
  }

  async obtenerTimelineDelDia(): Promise<FrancoTimeline[]> {
    const slots: FrancoTimeline[] = [];
    const startHour = 6;
    const totalSlots = 32;
    const now = new Date();
    const nowSlot = ((now.getHours() - startHour) * 60 + now.getMinutes()) / 30;

    for (let i = 0; i < totalSlots; i++) {
      const h = startHour + Math.floor(i / 2);
      const m = i % 2 === 0 ? "00" : "30";
      const conteo = i < nowSlot - 1 ? 0 : Math.floor(Math.random() * 4);
      let propia = 0;
      let tercero = 0;
      let demorados = 0;
      for (let j = 0; j < conteo; j++) {
        const r = Math.random();
        if (r < 0.08) demorados++;
        else if (r < 0.55) propia++;
        else tercero++;
      }
      slots.push({
        hora: `${String(h).padStart(2, "0")}:${m}`,
        camionesPropia: propia,
        camionesTercero: tercero,
        camionesDemorados: demorados,
        esActual: Math.round(nowSlot) === i,
      });
    }
    return delay(slots);
  }

  async crearTurnoManual(datos: NuevoTurnoManual): Promise<Turno> {
    const horasDesdeCorte = Math.max(0, Math.round((Date.now() - new Date(datos.corteEn).getTime()) / 3600000));
    const nuevo: Turno = {
      id: siguienteIdTurno(),
      hora: horaConOffset(60),
      offsetMin: 60,
      patente: datos.patente,
      chofer: datos.telefono,
      finca: datos.codigoFinca,
      flota: null,
      horasDesdeCorte,
      prioridad: calcularPrioridadCorte(horasDesdeCorte),
      estado: "pendiente",
      canal: "manual",
      esperaMin: 0,
      corteEn: datos.corteEn,
      cargaTon: datos.cargaTon,
      creadoEn: new Date().toISOString(),
    };
    this.turnos.push(nuevo);
    return delay({ ...nuevo });
  }

  async reasignarHorario(id: string, nuevaHora: string): Promise<Turno> {
    const turno = this.buscar(id);
    turno.hora = nuevaHora;
    return delay({ ...turno });
  }

  async cancelarTurno(id: string): Promise<Turno> {
    const turno = this.buscar(id);
    if (turno.estado === "completado" || turno.estado === "cancelado") {
      throw new Error("El turno ya está finalizado o cancelado.");
    }
    turno.estado = "cancelado";
    return delay({ ...turno });
  }

  async avanzarEstado(id: string): Promise<Turno> {
    const turno = this.buscar(id);
    const siguiente = SIGUIENTE_ESTADO[turno.estado];
    if (!siguiente) throw new Error("El turno no tiene un estado siguiente.");
    turno.estado = siguiente;
    return delay({ ...turno });
  }

  async alternarEstadoMolienda(): Promise<MetricasPanel> {
    this.moliendaOperando = !this.moliendaOperando;
    return this.obtenerMetricasPanel();
  }

  private buscar(id: string): Turno {
    const turno = this.turnos.find((t) => t.id === id);
    if (!turno) throw new Error("Turno no encontrado");
    return turno;
  }
}

// Se re-exporta por conveniencia para quien quiera generar horas de prueba fuera del repo.
export { horaConOffset, calcularPrioridadCorte };

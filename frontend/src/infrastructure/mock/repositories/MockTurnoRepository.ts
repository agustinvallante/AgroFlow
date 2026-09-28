import { calcularPrioridadCorte, type NuevoTurnoManual, type Turno } from "../../../domain/entities/Turno";
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
 * El día de mañana, para conectar al backend real, alcanza con
 * crear una HttpTurnoRepository que implemente esta misma interfaz
 * y cambiar una línea en `src/composition/container.ts`.
 */
export class MockTurnoRepository implements TurnoRepository {
  private turnos: Turno[] = generarTurnosSemilla();
  private moliendaOperando = true;

  async obtenerTurnosDelDia(): Promise<Turno[]> {
    return delay([...this.turnos]);
  }

  async obtenerMetricasPanel(): Promise<MetricasPanel> {
    const activos = this.turnos.filter((t) =>
      ["viaje", "cancha", "descargando"].includes(t.estado)
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
    const nuevo: Turno = {
      id: siguienteIdTurno(),
      hora: datos.hora,
      offsetMin: 9999,
      patente: datos.patente,
      chofer: datos.chofer,
      finca: datos.finca,
      flota: datos.flota,
      horasDesdeCorte: datos.horasDesdeCorte,
      estado: "pendiente",
      canal: "manual",
      esperaMin: 0,
    };
    this.turnos.push(nuevo);
    return delay(nuevo);
  }

  async reasignarHorario(id: string, nuevaHora: string): Promise<Turno> {
    const turno = this.turnos.find((t) => t.id === id);
    if (!turno) throw new Error("Turno no encontrado");
    turno.hora = nuevaHora;
    return delay(turno);
  }

  async cancelarTurno(id: string): Promise<void> {
    this.turnos = this.turnos.filter((t) => t.id !== id);
    return delay(undefined);
  }

  async avanzarEstado(id: string): Promise<Turno> {
    const turno = this.turnos.find((t) => t.id === id);
    if (!turno) throw new Error("Turno no encontrado");
    if (turno.estado === "pendiente") turno.estado = "viaje";
    else if (turno.estado === "viaje") turno.estado = "cancha";
    else if (turno.estado === "cancha") turno.estado = "descargando";
    else if (turno.estado === "descargando") turno.estado = "completado";
    return delay(turno);
  }

  async alternarEstadoMolienda(): Promise<MetricasPanel> {
    this.moliendaOperando = !this.moliendaOperando;
    return this.obtenerMetricasPanel();
  }
}

// Se re-exporta por conveniencia para quien quiera generar horas de prueba fuera del repo.
export { horaConOffset, calcularPrioridadCorte };

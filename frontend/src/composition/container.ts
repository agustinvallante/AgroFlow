import { HttpTurnoRepository } from "../infrastructure/http/repositories/HttpTurnoRepository";

import { MockTurnoRepository } from "../infrastructure/mock/repositories/MockTurnoRepository";
import { MockTransportistaRepository } from "../infrastructure/mock/repositories/MockTransportistaRepository";
import { MockConversacionRepository } from "../infrastructure/mock/repositories/MockConversacionRepository";
import { MockReporteRepository } from "../infrastructure/mock/repositories/MockReporteRepository";
import { MockConfiguracionRepository } from "../infrastructure/mock/repositories/MockConfiguracionRepository";

import type { TurnoRepository } from "../domain/repositories/TurnoRepository";

import {
  AlternarEstadoMolienda,
  ObtenerMetricasPanel,
  ObtenerTimelineDelDia,
} from "../application/usecases/panel/PanelUseCases";
import {
  AvanzarEstadoTurno,
  CancelarTurno,
  CrearTurnoManual,
  ObtenerDetalleTurno,
  ObtenerTurnosDelDia,
  ReasignarHorarioTurno,
} from "../application/usecases/turnos/TurnoUseCases";
import {
  CambiarEstadoActivoTransportista,
  CrearTransportista,
  ObtenerTransportistas,
} from "../application/usecases/transportistas/TransportistaUseCases";
import {
  GuardarPlantillas,
  ObtenerConversaciones,
  ObtenerFlujoDeAutomatizacion,
  ObtenerMetricasChatbot,
  ObtenerPlantillas,
} from "../application/usecases/conversaciones/ConversacionUseCases";
import { ObtenerReporte } from "../application/usecases/reportes/ObtenerReporte";
import {
  GuardarDatosIngenio,
  GuardarNotificaciones,
  GuardarReglasNegocio,
  ObtenerConfiguracion,
  ProbarConexionN8n,
} from "../application/usecases/configuracion/ConfiguracionUseCases";

export type FuenteDatos = "mock" | "http";

/**
 * Raíz de composición: el único archivo que decide qué implementación
 * concreta se usa.
 *
 *   VITE_DATA_SOURCE=mock (o sin definir) → todo mock, funciona sin backend
 *   VITE_DATA_SOURCE=http                 → turnos contra AgroFlow API
 *
 * El navegador llama a /api en su propio origen y el proxy de Vite
 * (vite.config.ts) lo reenvía a VITE_API_URL, por defecto
 * http://localhost:5000. Así no interviene CORS.
 *
 * La demo local solo expone turnos. Panel (métricas, timeline, molienda),
 * transportistas, conversaciones, reportes y configuración siguen en mock
 * y la UI los identifica como datos de demostración en modo http.
 */
function crearContainer() {
  const fuenteDatos: FuenteDatos = import.meta.env.VITE_DATA_SOURCE === "http" ? "http" : "mock";
  // Solo informativo: las llamadas van al mismo origen ("") vía proxy.
  const apiUrl = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

  // El tipo declarado es la INTERFAZ, no la clase concreta.
  const turnoRepository: TurnoRepository =
    fuenteDatos === "http" ? new HttpTurnoRepository("") : new MockTurnoRepository();

  // Con fuente http el panel usa su propio mock para no mezclar datos
  // simulados con los turnos persistidos por la API.
  const panelRepository: TurnoRepository =
    fuenteDatos === "http" ? new MockTurnoRepository() : turnoRepository;

  const transportistaRepository = new MockTransportistaRepository();
  const conversacionRepository = new MockConversacionRepository();
  const reporteRepository = new MockReporteRepository();
  const configuracionRepository = new MockConfiguracionRepository();

  return {
    fuenteDatos,
    apiUrl,
    panel: {
      obtenerMetricasPanel: new ObtenerMetricasPanel(panelRepository),
      obtenerTimelineDelDia: new ObtenerTimelineDelDia(panelRepository),
      alternarEstadoMolienda: new AlternarEstadoMolienda(panelRepository),
    },
    turnos: {
      obtenerTurnosDelDia: new ObtenerTurnosDelDia(turnoRepository),
      obtenerDetalleTurno: new ObtenerDetalleTurno(turnoRepository),
      crearTurnoManual: new CrearTurnoManual(turnoRepository),
      reasignarHorarioTurno: new ReasignarHorarioTurno(turnoRepository),
      cancelarTurno: new CancelarTurno(turnoRepository),
      avanzarEstadoTurno: new AvanzarEstadoTurno(turnoRepository),
    },
    transportistas: {
      obtenerTransportistas: new ObtenerTransportistas(transportistaRepository),
      crearTransportista: new CrearTransportista(transportistaRepository),
      cambiarEstadoActivoTransportista: new CambiarEstadoActivoTransportista(transportistaRepository),
    },
    conversaciones: {
      obtenerConversaciones: new ObtenerConversaciones(conversacionRepository),
      obtenerMetricasChatbot: new ObtenerMetricasChatbot(conversacionRepository),
      obtenerFlujoDeAutomatizacion: new ObtenerFlujoDeAutomatizacion(conversacionRepository),
      obtenerPlantillas: new ObtenerPlantillas(conversacionRepository),
      guardarPlantillas: new GuardarPlantillas(conversacionRepository),
    },
    reportes: {
      obtenerReporte: new ObtenerReporte(reporteRepository),
    },
    configuracion: {
      obtenerConfiguracion: new ObtenerConfiguracion(configuracionRepository),
      guardarDatosIngenio: new GuardarDatosIngenio(configuracionRepository),
      guardarReglasNegocio: new GuardarReglasNegocio(configuracionRepository),
      guardarNotificaciones: new GuardarNotificaciones(configuracionRepository),
      probarConexionN8n: new ProbarConexionN8n(configuracionRepository),
    },
  };
}

export const container = crearContainer();
export type Container = typeof container;

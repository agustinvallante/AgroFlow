import { MockTurnoRepository } from "../infrastructure/mock/repositories/MockTurnoRepository";
import { MockTransportistaRepository } from "../infrastructure/mock/repositories/MockTransportistaRepository";
import { MockConversacionRepository } from "../infrastructure/mock/repositories/MockConversacionRepository";
import { MockReporteRepository } from "../infrastructure/mock/repositories/MockReporteRepository";
import { MockConfiguracionRepository } from "../infrastructure/mock/repositories/MockConfiguracionRepository";

import {
  AlternarEstadoMolienda,
  ObtenerMetricasPanel,
  ObtenerTimelineDelDia,
} from "../application/usecases/panel/PanelUseCases";
import {
  AvanzarEstadoTurno,
  CancelarTurno,
  CrearTurnoManual,
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

/**
 * Raíz de composición (composition root).
 *
 * Este es el ÚNICO archivo del proyecto que decide qué implementación
 * concreta de cada repositorio se usa. Hoy instancia los repositorios
 * mock (en memoria, sin backend). El día que exista una API real, esto
 * se reemplaza por HttpTurnoRepository, HttpTransportistaRepository, etc.
 * y el resto del proyecto (casos de uso, hooks, componentes) no cambia
 * ni una línea, porque todos dependen de las interfaces del dominio,
 * no de esta implementación.
 */
function crearContainer() {
  // --- Repositorios (hoy: mock. mañana: HTTP contra el backend real) ---
  const turnoRepository = new MockTurnoRepository();
  const transportistaRepository = new MockTransportistaRepository();
  const conversacionRepository = new MockConversacionRepository();
  const reporteRepository = new MockReporteRepository();
  const configuracionRepository = new MockConfiguracionRepository();

  return {
    panel: {
      obtenerMetricasPanel: new ObtenerMetricasPanel(turnoRepository),
      obtenerTimelineDelDia: new ObtenerTimelineDelDia(turnoRepository),
      alternarEstadoMolienda: new AlternarEstadoMolienda(turnoRepository),
    },
    turnos: {
      obtenerTurnosDelDia: new ObtenerTurnosDelDia(turnoRepository),
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

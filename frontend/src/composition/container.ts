import { HttpTurnoRepository } from "../infrastructure/http/repositories/HttpTurnoRepository";
import { HttpTransportistaRepository } from "../infrastructure/http/repositories/HttpTransportistaRepository";
import { HttpConversacionRepository } from "../infrastructure/http/repositories/HttpConversacionRepository";
import { HttpVentanaRepository } from "../infrastructure/http/repositories/HttpVentanaRepository";
import { HttpAsociacionRepository } from "../infrastructure/http/repositories/HttpAsociacionRepository";
import { HttpFincaRepository } from "../infrastructure/http/repositories/HttpFincaRepository";
import { ApiClient } from "../infrastructure/http/ApiClient";
import { ObtenerFincas } from "../application/usecases/fincas/ObtenerFincas";
import { ObtenerVentanas } from "../application/usecases/ventanas/ObtenerVentanas";

interface DatosIngenio {
  name: string;
  timeZoneId: string;
  localDate: string;
}

import {
  CambiarEstadoTurno,
  CancelarTurno,
  CrearTurnoManual,
  ObtenerDetalleTurno,
  ObtenerTurnosDelDia,
  ObtenerTurnosActivos,
} from "../application/usecases/turnos/TurnoUseCases";
import {
  CambiarEstadoActivoTransportista,
  CrearTransportista,
  EditarTransportista,
  ObtenerTransportistas,
} from "../application/usecases/transportistas/TransportistaUseCases";
import {
  AsociarCamion,
  DesasociarCamion,
  ObtenerFlotaAsociada,
} from "../application/usecases/transportistas/AsociacionUseCases";
import {
  ObtenerConversaciones,
  ObtenerMensajesConversacion,
} from "../application/usecases/conversaciones/ConversacionUseCases";

/**
 * Raíz de composición: el único archivo que decide qué implementación
 * concreta se usa. El dashboard muestra únicamente datos de ZafraFlow API;
 * no hay repositorios mock ni datos de ejemplo.
 *
 * El navegador llama a /api en su propio origen y el proxy de Vite
 * (vite.config.ts) lo reenvía a VITE_API_URL, por defecto
 * http://localhost:5000. Así no interviene CORS.
 *
 * Catálogos, asociaciones, conversaciones, ventanas y datos del ingenio
 * usan la clave que el proxy agrega del lado servidor (server/dashboardProxy.ts).
 */
function crearContainer() {
  // Solo informativo: las llamadas van al mismo origen ("") vía proxy.
  const apiUrl = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

  const turnoRepository = new HttpTurnoRepository("");
  const transportistaRepository = new HttpTransportistaRepository("");
  const conversacionRepository = new HttpConversacionRepository("");
  const ventanaRepository = new HttpVentanaRepository("");
  const asociacionRepository = new HttpAsociacionRepository("");
  const fincaRepository = new HttpFincaRepository("");
  const api = new ApiClient("");

  return {
    apiUrl,
    ingenio: {
      obtener: () => api.get<DatosIngenio>("/api/v1/mill"),
    },
    ventanas: {
      obtenerVentanas: new ObtenerVentanas(ventanaRepository),
    },
    turnos: {
      obtenerEstadosDelDia: (fecha: string) => turnoRepository.obtenerEstadosDelDia(fecha),
      obtenerTurnosDelDia: new ObtenerTurnosDelDia(turnoRepository),
      obtenerTurnosActivos: new ObtenerTurnosActivos(turnoRepository),
      obtenerDetalleTurno: new ObtenerDetalleTurno(turnoRepository),
      crearTurnoManual: new CrearTurnoManual(turnoRepository),
      cancelarTurno: new CancelarTurno(turnoRepository),
      cambiarEstadoTurno: new CambiarEstadoTurno(turnoRepository),
    },
    transportistas: {
      obtenerTransportistas: new ObtenerTransportistas(transportistaRepository),
      crearTransportista: new CrearTransportista(transportistaRepository),
      editarTransportista: new EditarTransportista(transportistaRepository),
      cambiarEstadoActivoTransportista: new CambiarEstadoActivoTransportista(transportistaRepository),
    },
    fincas: {
      obtenerFincas: new ObtenerFincas(fincaRepository),
    },
    asociaciones: {
      obtenerFlotaAsociada: new ObtenerFlotaAsociada(asociacionRepository),
      asociarCamion: new AsociarCamion(asociacionRepository),
      desasociarCamion: new DesasociarCamion(asociacionRepository),
    },
    conversaciones: {
      obtenerConversaciones: new ObtenerConversaciones(conversacionRepository),
      obtenerMensajesConversacion: new ObtenerMensajesConversacion(conversacionRepository),
    },
  };
}

export const container = crearContainer();
export type Container = typeof container;

import { HttpTurnoRepository } from "../infrastructure/http/repositories/HttpTurnoRepository";

import {
  CambiarEstadoTurno,
  CancelarTurno,
  CrearTurnoManual,
  ObtenerDetalleTurno,
  ObtenerTurnosDelDia,
} from "../application/usecases/turnos/TurnoUseCases";

/**
 * Raíz de composición: el único archivo que decide qué implementación
 * concreta se usa. El dashboard muestra únicamente datos de AgroFlow API;
 * no hay repositorios mock ni datos de ejemplo.
 *
 * El navegador llama a /api en su propio origen y el proxy de Vite
 * (vite.config.ts) lo reenvía a VITE_API_URL, por defecto
 * http://localhost:5000. Así no interviene CORS.
 *
 * Métricas del panel, chatbot, transportistas, reportes y configuración no
 * tienen endpoint en openapi.yaml: sus vistas conservan el diseño, sin datos.
 */
function crearContainer() {
  // Solo informativo: las llamadas van al mismo origen ("") vía proxy.
  const apiUrl = import.meta.env.VITE_API_URL ?? "http://localhost:5000";

  const turnoRepository = new HttpTurnoRepository("");

  return {
    apiUrl,
    turnos: {
      obtenerTurnosDelDia: new ObtenerTurnosDelDia(turnoRepository),
      obtenerDetalleTurno: new ObtenerDetalleTurno(turnoRepository),
      crearTurnoManual: new CrearTurnoManual(turnoRepository),
      cancelarTurno: new CancelarTurno(turnoRepository),
      cambiarEstadoTurno: new CambiarEstadoTurno(turnoRepository),
    },
  };
}

export const container = crearContainer();
export type Container = typeof container;

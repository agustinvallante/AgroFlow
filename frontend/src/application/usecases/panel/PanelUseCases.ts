import type { TurnoRepository } from "../../../domain/repositories/TurnoRepository";

/** Trae las métricas resumen del panel general (camiones en espera, molienda, etc). */
export class ObtenerMetricasPanel {
  private readonly repo: TurnoRepository;

  constructor(repo: TurnoRepository) {
    this.repo = repo;
  }
  execute() {
    return this.repo.obtenerMetricasPanel();
  }
}

/** Trae la cola virtual del día para la visualización de línea de tiempo. */
export class ObtenerTimelineDelDia {
  private readonly repo: TurnoRepository;

  constructor(repo: TurnoRepository) {
    this.repo = repo;
  }
  execute() {
    return this.repo.obtenerTimelineDelDia();
  }
}

/** Alterna el estado de la molienda (operando/detenida) y dispara la difusión de aviso. */
export class AlternarEstadoMolienda {
  private readonly repo: TurnoRepository;

  constructor(repo: TurnoRepository) {
    this.repo = repo;
  }
  execute() {
    return this.repo.alternarEstadoMolienda();
  }
}

import type { NuevoTurnoManual, Turno } from "../../../domain/entities/Turno";
import type { TurnoRepository } from "../../../domain/repositories/TurnoRepository";

export class ObtenerTurnosDelDia {
  private readonly repo: TurnoRepository;

  constructor(repo: TurnoRepository) {
    this.repo = repo;
  }
  execute(): Promise<Turno[]> {
    return this.repo.obtenerTurnosDelDia();
  }
}

export class CrearTurnoManual {
  private readonly repo: TurnoRepository;

  constructor(repo: TurnoRepository) {
    this.repo = repo;
  }
  execute(datos: NuevoTurnoManual): Promise<Turno> {
    // Regla de negocio: un turno cargado a mano siempre entra como "pendiente".
    if (!datos.patente || !datos.chofer || !datos.finca || !datos.hora) {
      return Promise.reject(
        new Error("Completá al menos patente, transportista, finca y hora del turno.")
      );
    }
    return this.repo.crearTurnoManual({ ...datos, patente: datos.patente.toUpperCase() });
  }
}

export class ReasignarHorarioTurno {
  private readonly repo: TurnoRepository;

  constructor(repo: TurnoRepository) {
    this.repo = repo;
  }
  execute(id: string, nuevaHora: string): Promise<Turno> {
    return this.repo.reasignarHorario(id, nuevaHora);
  }
}

export class CancelarTurno {
  private readonly repo: TurnoRepository;

  constructor(repo: TurnoRepository) {
    this.repo = repo;
  }
  execute(id: string): Promise<void> {
    return this.repo.cancelarTurno(id);
  }
}

export class AvanzarEstadoTurno {
  private readonly repo: TurnoRepository;

  constructor(repo: TurnoRepository) {
    this.repo = repo;
  }
  execute(id: string): Promise<Turno> {
    return this.repo.avanzarEstado(id);
  }
}

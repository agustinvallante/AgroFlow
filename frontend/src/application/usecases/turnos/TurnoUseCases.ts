import type { FiltrosTurno, NuevoTurnoManual, Turno } from "../../../domain/entities/Turno";
import type { TurnoRepository } from "../../../domain/repositories/TurnoRepository";

export class ObtenerTurnosDelDia {
  private readonly repo: TurnoRepository;

  constructor(repo: TurnoRepository) {
    this.repo = repo;
  }
  execute(filtros?: FiltrosTurno): Promise<Turno[]> {
    return this.repo.obtenerTurnosDelDia(filtros);
  }
}

export class ObtenerDetalleTurno {
  private readonly repo: TurnoRepository;

  constructor(repo: TurnoRepository) {
    this.repo = repo;
  }
  execute(id: string): Promise<Turno> {
    return this.repo.obtenerTurno(id);
  }
}

export class CrearTurnoManual {
  private readonly repo: TurnoRepository;

  constructor(repo: TurnoRepository) {
    this.repo = repo;
  }
  execute(datos: NuevoTurnoManual): Promise<Turno> {
    // Solo se controla que el formulario esté completo; la validación de
    // formato, referencias y capacidad es responsabilidad de la API.
    if (!datos.telefono || !datos.patente || !datos.codigoFinca || !datos.corteEn || !datos.cargaTon) {
      return Promise.reject(
        new Error("Completá teléfono, patente, código de finca, momento de corte y carga estimada.")
      );
    }
    return this.repo.crearTurnoManual({
      ...datos,
      telefono: datos.telefono.trim(),
      patente: datos.patente.trim().toUpperCase(),
      codigoFinca: datos.codigoFinca.trim().toUpperCase(),
    });
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
  execute(id: string): Promise<Turno> {
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

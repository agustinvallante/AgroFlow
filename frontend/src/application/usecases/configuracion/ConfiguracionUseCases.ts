import type { Configuracion } from "../../../domain/entities/Configuracion";
import type { ConfiguracionRepository } from "../../../domain/repositories/ConfiguracionRepository";

export class ObtenerConfiguracion {
  private readonly repo: ConfiguracionRepository;

  constructor(repo: ConfiguracionRepository) {
    this.repo = repo;
  }
  execute() {
    return this.repo.obtenerConfiguracion();
  }
}

export class GuardarDatosIngenio {
  private readonly repo: ConfiguracionRepository;

  constructor(repo: ConfiguracionRepository) {
    this.repo = repo;
  }
  execute(datos: Configuracion["ingenio"]) {
    return this.repo.guardarDatosIngenio(datos);
  }
}

export class GuardarReglasNegocio {
  private readonly repo: ConfiguracionRepository;

  constructor(repo: ConfiguracionRepository) {
    this.repo = repo;
  }
  execute(reglas: Configuracion["reglas"]) {
    return this.repo.guardarReglasNegocio(reglas);
  }
}

export class GuardarNotificaciones {
  private readonly repo: ConfiguracionRepository;

  constructor(repo: ConfiguracionRepository) {
    this.repo = repo;
  }
  execute(notificaciones: Configuracion["notificaciones"]) {
    return this.repo.guardarNotificaciones(notificaciones);
  }
}

export class ProbarConexionN8n {
  private readonly repo: ConfiguracionRepository;

  constructor(repo: ConfiguracionRepository) {
    this.repo = repo;
  }
  execute() {
    return this.repo.probarConexionN8n();
  }
}

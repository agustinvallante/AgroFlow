import type { PlantillaMensaje } from "../../../domain/entities/Conversacion";
import type { ConversacionRepository } from "../../../domain/repositories/ConversacionRepository";

export class ObtenerConversaciones {
  private readonly repo: ConversacionRepository;

  constructor(repo: ConversacionRepository) {
    this.repo = repo;
  }
  execute() {
    return this.repo.obtenerConversaciones();
  }
}

export class ObtenerMetricasChatbot {
  private readonly repo: ConversacionRepository;

  constructor(repo: ConversacionRepository) {
    this.repo = repo;
  }
  execute() {
    return this.repo.obtenerMetricasChatbot();
  }
}

export class ObtenerFlujoDeAutomatizacion {
  private readonly repo: ConversacionRepository;

  constructor(repo: ConversacionRepository) {
    this.repo = repo;
  }
  execute() {
    return this.repo.obtenerFlujoDeAutomatizacion();
  }
}

export class ObtenerPlantillas {
  private readonly repo: ConversacionRepository;

  constructor(repo: ConversacionRepository) {
    this.repo = repo;
  }
  execute() {
    return this.repo.obtenerPlantillas();
  }
}

export class GuardarPlantillas {
  private readonly repo: ConversacionRepository;

  constructor(repo: ConversacionRepository) {
    this.repo = repo;
  }
  execute(plantillas: PlantillaMensaje[]) {
    return this.repo.guardarPlantillas(plantillas);
  }
}

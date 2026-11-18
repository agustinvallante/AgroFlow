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

export class ObtenerMensajesConversacion {
  private readonly repo: ConversacionRepository;

  constructor(repo: ConversacionRepository) {
    this.repo = repo;
  }
  execute(conversacionId: string) {
    return this.repo.obtenerMensajes(conversacionId);
  }
}

import type { Conversacion, MensajeChat } from "../entities/Conversacion";

export interface ConversacionRepository {
  obtenerConversaciones(): Promise<Conversacion[]>;
  /** Mensajes en el orden en que se persistieron. */
  obtenerMensajes(conversacionId: string): Promise<MensajeChat[]>;
}

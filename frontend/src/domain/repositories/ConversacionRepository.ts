import type {
  Conversacion,
  MensajeChat,
  MetricasChatbot,
  PasoFlujoBot,
  PlantillaMensaje,
} from "../entities/Conversacion";

export interface ConversacionRepository {
  obtenerConversaciones(): Promise<Conversacion[]>;
  /** Mensajes en el orden en que se persistieron. */
  obtenerMensajes(conversacionId: string): Promise<MensajeChat[]>;
  obtenerMetricasChatbot(): Promise<MetricasChatbot>;
  obtenerFlujoDeAutomatizacion(): Promise<PasoFlujoBot[]>;
  obtenerPlantillas(): Promise<PlantillaMensaje[]>;
  guardarPlantillas(plantillas: PlantillaMensaje[]): Promise<void>;
}

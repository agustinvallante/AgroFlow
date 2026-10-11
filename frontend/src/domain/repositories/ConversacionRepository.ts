import type {
  Conversacion,
  MetricasChatbot,
  PasoFlujoBot,
  PlantillaMensaje,
} from "../entities/Conversacion";

export interface ConversacionRepository {
  obtenerConversaciones(): Promise<Conversacion[]>;
  obtenerMetricasChatbot(): Promise<MetricasChatbot>;
  obtenerFlujoDeAutomatizacion(): Promise<PasoFlujoBot[]>;
  obtenerPlantillas(): Promise<PlantillaMensaje[]>;
  guardarPlantillas(plantillas: PlantillaMensaje[]): Promise<void>;
}

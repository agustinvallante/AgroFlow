import type {
  Conversacion,
  MetricasChatbot,
  PasoFlujoBot,
  PlantillaMensaje,
} from "../../../domain/entities/Conversacion";
import type { ConversacionRepository } from "../../../domain/repositories/ConversacionRepository";
import type { ApiClient } from "../ApiClient";
import type { ConversationMetricsDto, ConversationSummaryDto } from "../dto/backend.dto";
import { mapConversacion, mapMetricasChatbot } from "../mappers/backend.mappers";
import {
  generarFlujoBotSemilla,
  generarPlantillasSemilla,
} from "../../mock/data/conversaciones.mock";

/**
 * Repositorio híbrido, y conviene entender por qué:
 *
 *   - obtenerConversaciones() y obtenerMetricasChatbot() SÍ pegan a la API:
 *     el backend guarda el log de mensajes que escribe n8n.
 *
 *   - obtenerFlujoDeAutomatizacion() y las plantillas NO tienen endpoint.
 *     Son documentación del flujo de n8n, no datos operativos. Se sirven
 *     desde la misma semilla que usaba el mock.
 *
 * Que una parte sea real y otra estática es invisible para la vista: el
 * puerto es el mismo. Si mañana el backend expone /api/conversations/templates,
 * se reemplazan estos dos métodos y nada más cambia.
 */
export class HttpConversacionRepository implements ConversacionRepository {
  private plantillas: PlantillaMensaje[] = generarPlantillasSemilla();
  private readonly api: ApiClient;

  constructor(api: ApiClient) {
    this.api = api;
  }

  async obtenerConversaciones(): Promise<Conversacion[]> {
    const data = await this.api.get<ConversationSummaryDto[]>("/api/conversations");
    return data.map(mapConversacion);
  }

  async obtenerMetricasChatbot(): Promise<MetricasChatbot> {
    const data = await this.api.get<ConversationMetricsDto>("/api/conversations/metrics");
    return mapMetricasChatbot(data);
  }

  async obtenerFlujoDeAutomatizacion(): Promise<PasoFlujoBot[]> {
    return generarFlujoBotSemilla();
  }

  async obtenerPlantillas(): Promise<PlantillaMensaje[]> {
    return this.plantillas;
  }

  async guardarPlantillas(plantillas: PlantillaMensaje[]): Promise<void> {
    // Sin endpoint todavía: se guarda en memoria y se pierde al recargar.
    this.plantillas = plantillas;
  }
}

import type { Conversacion, MensajeChat, PlantillaMensaje } from "../../../domain/entities/Conversacion";
import type { ConversacionRepository } from "../../../domain/repositories/ConversacionRepository";
import { ApiClient } from "../ApiClient";
import type { ConversationMessageDto, ConversationSummaryDto, PageDto } from "../dto/ConversationDto";
import { mapConversationMessage, mapConversationSummary } from "../mappers/conversationMapper";

const BASE = "/api/v1/conversations";
const PAGE_SIZE = "50";
/** Tope de páginas por conversación: 1000 mensajes alcanzan para la demo. */
const MAX_MESSAGE_PAGES = 20;

/**
 * Conversaciones y mensajes persistidos por AgroFlow API. Métricas, flujo y
 * plantillas no existen en la API de la demo: se delegan a la fuente de
 * demostración recibida y la vista los identifica como tales.
 */
export class HttpConversacionRepository implements ConversacionRepository {
  private readonly api: ApiClient;
  private readonly demo: ConversacionRepository;

  constructor(baseUrl: string, demo: ConversacionRepository) {
    this.api = new ApiClient(baseUrl);
    this.demo = demo;
  }

  async obtenerConversaciones(): Promise<Conversacion[]> {
    const page = await this.api.get<PageDto<ConversationSummaryDto>>(BASE, { limit: PAGE_SIZE });
    return page.items.map(mapConversationSummary);
  }

  async obtenerMensajes(conversacionId: string): Promise<MensajeChat[]> {
    const mensajes: MensajeChat[] = [];
    let cursor: string | undefined;
    for (let pagina = 0; pagina < MAX_MESSAGE_PAGES; pagina++) {
      const page = await this.api.get<PageDto<ConversationMessageDto>>(
        `${BASE}/${encodeURIComponent(conversacionId)}/messages`,
        { limit: PAGE_SIZE, cursor },
      );
      mensajes.push(...page.items.map(mapConversationMessage));
      if (!page.nextCursor) break;
      cursor = page.nextCursor;
    }
    return mensajes;
  }

  obtenerMetricasChatbot() {
    return this.demo.obtenerMetricasChatbot();
  }

  obtenerFlujoDeAutomatizacion() {
    return this.demo.obtenerFlujoDeAutomatizacion();
  }

  obtenerPlantillas() {
    return this.demo.obtenerPlantillas();
  }

  guardarPlantillas(plantillas: PlantillaMensaje[]) {
    return this.demo.guardarPlantillas(plantillas);
  }
}

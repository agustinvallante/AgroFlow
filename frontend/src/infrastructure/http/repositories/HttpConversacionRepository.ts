import type { Conversacion, MensajeChat } from "../../../domain/entities/Conversacion";
import { ListadoIncompleto } from "../../../domain/errors/ListadoIncompleto";
import type { ConversacionRepository } from "../../../domain/repositories/ConversacionRepository";
import { ApiClient } from "../ApiClient";
import type { ConversationMessageDto, ConversationSummaryDto, PageDto } from "../dto/ConversationDto";
import { mapConversationMessage, mapConversationSummary } from "../mappers/conversationMapper";

const BASE = "/api/v1/conversations";
const PAGE_SIZE = "50";
/** Tope de páginas por lectura: 1000 conversaciones o mensajes alcanzan para la demo. */
const MAX_PAGES = 20;

/**
 * Sigue `nextCursor` hasta el final. Si falla la primera página se propaga el
 * error; si falla una posterior o se alcanza el tope, se lanza
 * {@link ListadoIncompleto} con lo ya leído.
 */
async function recorrerPaginas<D, T>(
  pedir: (cursor: string | undefined) => Promise<PageDto<D>>,
  mapear: (dto: D) => T,
  nombre: string,
): Promise<T[]> {
  const items: T[] = [];
  let cursor: string | undefined;
  for (let pagina = 0; pagina < MAX_PAGES; pagina++) {
    let page: PageDto<D>;
    try {
      page = await pedir(cursor);
    } catch (e) {
      if (pagina === 0) throw e;
      const causa = e instanceof Error && e.message ? ` ${e.message}` : "";
      throw new ListadoIncompleto(items, `Se muestran ${items.length} ${nombre}; no se pudo cargar el resto.${causa}`);
    }
    items.push(...page.items.map(mapear));
    if (!page.nextCursor) return items;
    cursor = page.nextCursor;
  }
  throw new ListadoIncompleto(items, `Se muestran los primeros ${items.length} ${nombre}.`);
}

/** Conversaciones y mensajes persistidos por ZafraFlow API. */
export class HttpConversacionRepository implements ConversacionRepository {
  private readonly api: ApiClient;

  constructor(baseUrl: string) {
    this.api = new ApiClient(baseUrl);
  }

  obtenerConversaciones(): Promise<Conversacion[]> {
    return recorrerPaginas(
      (cursor) => this.api.get<PageDto<ConversationSummaryDto>>(BASE, { limit: PAGE_SIZE, cursor }),
      mapConversationSummary,
      "conversaciones",
    );
  }

  obtenerMensajes(conversacionId: string): Promise<MensajeChat[]> {
    return recorrerPaginas(
      (cursor) =>
        this.api.get<PageDto<ConversationMessageDto>>(`${BASE}/${encodeURIComponent(conversacionId)}/messages`, {
          limit: PAGE_SIZE,
          cursor,
        }),
      mapConversationMessage,
      "mensajes",
    );
  }
}

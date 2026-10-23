import type {
  Conversacion,
  EstadoEnvio,
  EstadoProcesamiento,
  MensajeChat,
} from "../../../domain/entities/Conversacion";
import type {
  ConversationMessageDto,
  ConversationSummaryDto,
  ProcessingStatusDto,
  SendStatusDto,
} from "../dto/ConversationDto";
import { horaEnZonaIngenio } from "../../../shared/utils/date";

const ENVIO: Record<SendStatusDto, EstadoEnvio> = {
  PENDING_SEND: "pendiente",
  ACCEPTED: "aceptado",
  FAILED: "fallido",
  UNKNOWN: "incierto",
};

const PROCESAMIENTO: Record<ProcessingStatusDto, EstadoProcesamiento> = {
  PROCESSING: "en-curso",
  COMPLETED: "completado",
  FAILED: "fallido",
  EXPIRED: "vencido",
};

/**
 * El nombre sólo existe para un transportista activo reconocido; cualquier
 * otro remitente se muestra con la dirección enmascarada por la API.
 */
export function mapConversationSummary(dto: ConversationSummaryDto): Conversacion {
  const reconocido = dto.sender.recognized && Boolean(dto.sender.name);
  return {
    id: dto.id,
    nombre: reconocido ? dto.sender.name! : `Remitente no registrado · ${dto.remoteAddressMasked}`,
    hora: horaEnZonaIngenio(dto.lastMessageAt),
    preview: dto.lastMessagePreview ?? "",
    // "escalado" del mock no se emula: la API sólo informa si requiere revisión.
    estado: dto.needsAttention ? "revision" : "bot",
    mensajes: [],
    remitenteReconocido: reconocido,
    direccion: dto.remoteAddressMasked,
  };
}

export function mapConversationMessage(dto: ConversationMessageDto): MensajeChat {
  return {
    id: dto.id,
    autor: dto.direction === "INBOUND" ? "transportista" : "bot",
    texto: dto.text,
    hora: horaEnZonaIngenio(dto.persistedAt),
    estadoEnvio: dto.sendStatus ? ENVIO[dto.sendStatus] : undefined,
    procesamiento: dto.processingStatus ? PROCESAMIENTO[dto.processingStatus] : undefined,
  };
}

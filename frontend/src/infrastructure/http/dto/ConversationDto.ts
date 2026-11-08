/** Esquemas de docs/contracts/openapi.yaml (etiqueta Conversaciones). Fechas en UTC. */

export type SendStatusDto = "PENDING_SEND" | "ACCEPTED" | "FAILED" | "UNKNOWN";
export type ProcessingStatusDto = "PROCESSING" | "COMPLETED" | "FAILED" | "EXPIRED";
export type MessageDirectionDto = "INBOUND" | "OUTBOUND";

export interface ConversationSummaryDto {
  id: string;
  addressKind: "PHONE" | "LID" | "OTHER";
  remoteAddressMasked: string;
  sender: { recognized: boolean; transportistaId: string | null; name: string | null };
  lastMessageAt: string;
  lastMessagePreview: string | null;
  lastMessageDirection: MessageDirectionDto | null;
  needsAttention: boolean;
}

export interface ConversationMessageDto {
  id: string;
  direction: MessageDirectionDto;
  text: string;
  providerTimestamp: string | null;
  persistedAt: string;
  inReplyToMessageId: string | null;
  sendStatus: SendStatusDto | null;
  processingStatus: ProcessingStatusDto | null;
}

export interface PageDto<T> {
  items: T[];
  nextCursor: string | null;
}

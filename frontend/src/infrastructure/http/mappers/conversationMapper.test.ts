import { describe, expect, it } from "vitest";
import { mapConversationMessage, mapConversationSummary } from "./conversationMapper";
import { mapDriver, toDriverInput } from "./driverMapper";
import type { ConversationMessageDto, ConversationSummaryDto } from "../dto/ConversationDto";

const resumen: ConversationSummaryDto = {
  id: "c1",
  addressKind: "PHONE",
  remoteAddressMasked: "+54•••••••0155",
  sender: { recognized: true, transportistaId: "d1", name: "Transportista Sintético" },
  lastMessageAt: "2026-01-01T15:00:00Z",
  lastMessagePreview: "[SINTÉTICO] Hola",
  lastMessageDirection: "OUTBOUND",
  needsAttention: false,
};

const mensaje: ConversationMessageDto = {
  id: "m1",
  direction: "INBOUND",
  text: "[SINTÉTICO] Hola",
  providerTimestamp: "2026-01-01T14:59:58Z",
  persistedAt: "2026-01-01T15:00:00Z",
  inReplyToMessageId: null,
  sendStatus: null,
  processingStatus: "COMPLETED",
};

describe("conversationMapper", () => {
  it("muestra el nombre del transportista reconocido y la hora del ingenio", () => {
    const conversacion = mapConversationSummary(resumen);

    expect(conversacion.nombre).toBe("Transportista Sintético");
    expect(conversacion.remitenteReconocido).toBe(true);
    // 15:00 UTC son las 12:00 en Tucumán (UTC-03:00).
    expect(conversacion.hora).toBe("12:00");
    expect(conversacion.estado).toBe("bot");
    expect(conversacion.mensajes).toEqual([]);
  });

  it("identifica al remitente no registrado sólo por la dirección enmascarada", () => {
    const conversacion = mapConversationSummary({
      ...resumen,
      sender: { recognized: false, transportistaId: null, name: null },
      needsAttention: true,
    });

    expect(conversacion.nombre).toBe("Remitente no registrado · +54•••••••0155");
    expect(conversacion.remitenteReconocido).toBe(false);
    expect(conversacion.estado).toBe("revision");
  });

  it("traduce dirección, estado de envío y procesamiento sin inventar una entrega", () => {
    expect(mapConversationMessage(mensaje)).toMatchObject({ autor: "transportista", procesamiento: "completado", hora: "12:00" });
    const casos = [
      ["PENDING_SEND", "pendiente"],
      ["ACCEPTED", "aceptado"],
      ["FAILED", "fallido"],
      ["UNKNOWN", "incierto"],
    ] as const;
    for (const [api, dominio] of casos) {
      const saliente = mapConversationMessage({ ...mensaje, direction: "OUTBOUND", sendStatus: api, processingStatus: null });
      expect(saliente.autor).toBe("bot");
      expect(saliente.estadoEnvio).toBe(dominio);
    }
  });
});

describe("driverMapper", () => {
  it("no inventa patente, finca ni flota que la API no publica", () => {
    expect(
      mapDriver({
        id: "d1", name: "T", dni: "20555555555", whatsApp: "+5493815550155", isActive: true,
        activeTruckPlates: [], canRequestAppointments: false,
      }),
    ).toEqual({
      id: "d1",
      nombre: "T",
      dni: "20555555555",
      telefono: "+5493815550155",
      patente: null,
      finca: null,
      flota: null,
      turnosEsteMes: null,
      activo: true,
      puedeSolicitarTurnos: false,
    });
  });

  it("muestra el camión asociado y si puede solicitar turnos según la API", () => {
    const conCamion = mapDriver({
      id: "d1", name: "T", dni: "1", whatsApp: "+5493815550101", isActive: true,
      activeTruckPlates: ["AF123BC"], canRequestAppointments: true,
    });
    expect(conCamion.patente).toBe("AF123BC");
    expect(conCamion.puedeSolicitarTurnos).toBe(true);
  });

  it("envía sólo los campos del contrato", () => {
    expect(
      toDriverInput({ nombre: " T ", dni: " 20555555555 ", telefono: "+5493815550155", patente: "", finca: "", flota: "propia" }),
    ).toEqual({ name: "T", dni: "20555555555", whatsApp: "+5493815550155" });
  });
});

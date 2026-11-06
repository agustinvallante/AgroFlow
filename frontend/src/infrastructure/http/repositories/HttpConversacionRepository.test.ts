import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { HttpConversacionRepository } from "./HttpConversacionRepository";
import { HttpTransportistaRepository } from "./HttpTransportistaRepository";
import { MockConversacionRepository } from "../../mock/repositories/MockConversacionRepository";
import { ApiError } from "../ApiClient";
import { CrearTransportista } from "../../../application/usecases/transportistas/TransportistaUseCases";

const respuesta = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json" } });

const mensaje = (id: string) => ({
  id,
  direction: "INBOUND",
  text: `[SINTÉTICO] ${id}`,
  providerTimestamp: null,
  persistedAt: "2026-01-01T15:00:00Z",
  inReplyToMessageId: null,
  sendStatus: null,
  processingStatus: "PROCESSING",
});

const driver = {
  id: "d1",
  name: "Transportista Sintético",
  dni: "20555555555",
  whatsApp: "+5493815550155",
  isActive: true,
  activeTruckPlates: [],
  canRequestAppointments: false,
};

describe("repositorios HTTP del dashboard", () => {
  const fetchMock = vi.fn<typeof fetch>();

  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("lista conversaciones del mismo origen, sin enviar ninguna clave desde el navegador", async () => {
    fetchMock.mockResolvedValueOnce(respuesta(200, { items: [], nextCursor: null }));

    await new HttpConversacionRepository("", new MockConversacionRepository()).obtenerConversaciones();

    const [url, init] = fetchMock.mock.calls[0];
    expect(String(url)).toMatch(/\/api\/v1\/conversations\?limit=50$/);
    expect(JSON.stringify(init?.headers ?? {})).not.toMatch(/agroflow/i);
  });

  it("recorre todas las páginas de mensajes siguiendo el cursor opaco", async () => {
    fetchMock
      .mockResolvedValueOnce(respuesta(200, { items: [mensaje("m1"), mensaje("m2")], nextCursor: "cursor-1" }))
      .mockResolvedValueOnce(respuesta(200, { items: [mensaje("m3")], nextCursor: null }));

    const mensajes = await new HttpConversacionRepository("", new MockConversacionRepository()).obtenerMensajes("c1");

    expect(mensajes.map((m) => m.id)).toEqual(["m1", "m2", "m3"]);
    expect(String(fetchMock.mock.calls[1][0])).toContain("cursor=cursor-1");
  });

  it("informa la falta de clave del dashboard con un mensaje accionable", async () => {
    fetchMock.mockResolvedValueOnce(respuesta(401, { code: "UNAUTHENTICATED", title: "No autenticado" }));

    const error = await new HttpConversacionRepository("", new MockConversacionRepository())
      .obtenerConversaciones()
      .catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).message).toMatch(/AGROFLOW_DASHBOARD_KEY/);
  });

  it("registra un transportista con Idempotency-Key y devuelve lo persistido", async () => {
    fetchMock
      .mockResolvedValueOnce(respuesta(201, { resourceId: "d1", resourceType: "driver", outcome: "created" }))
      .mockResolvedValueOnce(respuesta(200, driver));
    const repo = new HttpTransportistaRepository("", () => "clave-idempotencia-1");

    const creado = await new CrearTransportista(repo).execute({
      nombre: "Transportista Sintético",
      dni: "20555555555",
      telefono: "+54 9 381 555-0155",
      patente: "",
      finca: "",
      flota: "propia",
    });

    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api/v1/drivers");
    expect(init?.method).toBe("POST");
    expect((init?.headers as Record<string, string>)["Idempotency-Key"]).toBe("clave-idempotencia-1");
    expect(JSON.parse(String(init?.body))).toEqual({ name: "Transportista Sintético", dni: "20555555555", whatsApp: "+5493815550155" });
    // Recién registrado: reconocido por el canal, pero sin camión no puede solicitar turnos.
    expect(creado).toMatchObject({ id: "d1", telefono: "+5493815550155", activo: true, patente: null, puedeSolicitarTurnos: false });
  });

  it("muestra el conflicto de WhatsApp duplicado", async () => {
    fetchMock.mockResolvedValueOnce(respuesta(409, { code: "DUPLICATE_WHATSAPP", title: "x" }));

    const error = await new HttpTransportistaRepository("")
      .crear({ nombre: "T", dni: "1", telefono: "+5493815550155", patente: "", finca: "", flota: "propia" })
      .catch((e: unknown) => e);

    expect((error as ApiError).message).toBe("Ya existe un transportista con ese WhatsApp.");
  });

  it("inhabilita a un transportista activo y no simula una reactivación inexistente", async () => {
    fetchMock
      .mockResolvedValueOnce(respuesta(200, driver))
      .mockResolvedValueOnce(respuesta(200, { resourceId: "d1", resourceType: "driver", outcome: "inactivated" }))
      .mockResolvedValueOnce(respuesta(200, { ...driver, isActive: false }));
    const repo = new HttpTransportistaRepository("", () => "clave-2");

    const inactivo = await repo.cambiarEstadoActivo("d1");
    expect(inactivo.activo).toBe(false);
    expect(fetchMock.mock.calls[1][0]).toBe("/api/v1/drivers/d1/inactivation");

    fetchMock.mockResolvedValueOnce(respuesta(200, { ...driver, isActive: false }));
    await expect(repo.cambiarEstadoActivo("d1")).rejects.toThrow(/reactivación/);
    expect(fetchMock).toHaveBeenCalledTimes(4);
  });
});

import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Conversacion, MensajeChat } from "../../domain/entities/Conversacion";

const mocks = vi.hoisted(() => ({
  obtenerConversaciones: vi.fn(),
  obtenerMensajes: vi.fn(),
}));

vi.mock("../../composition/container", () => ({
  container: {
    apiUrl: "http://localhost:5000",
    conversaciones: {
      obtenerConversaciones: { execute: mocks.obtenerConversaciones },
      obtenerMensajesConversacion: { execute: mocks.obtenerMensajes },
    },
  },
}));

import { ChatbotView } from "./ChatbotView";

const reconocida: Conversacion = {
  id: "c1",
  nombre: "Transportista Sintético",
  hora: "12:00",
  preview: "[SINTÉTICO] Respuesta",
  estado: "bot",
  mensajes: [],
  remitenteReconocido: true,
  direccion: "+54•••••••0155",
};

const noRegistrada: Conversacion = {
  id: "c2",
  nombre: "Remitente no registrado · +54•••••••0199",
  hora: "11:00",
  preview: "[SINTÉTICO] Hola",
  estado: "revision",
  mensajes: [],
  remitenteReconocido: false,
  direccion: "+54•••••••0199",
};

const mensajes: Record<string, MensajeChat[]> = {
  c1: [
    { id: "m1", autor: "transportista", texto: "[SINTÉTICO] Quiero mi turno", hora: "11:59", procesamiento: "completado" },
    { id: "m2", autor: "bot", texto: "[SINTÉTICO] Respuesta", hora: "12:00", estadoEnvio: "aceptado" },
  ],
  c2: [
    { id: "m3", autor: "transportista", texto: "[SINTÉTICO] Hola", hora: "11:00", procesamiento: "en-curso" },
    { id: "m4", autor: "bot", texto: "[SINTÉTICO] No registrado", hora: "11:00", estadoEnvio: "incierto" },
  ],
};

describe("ChatbotView con AgroFlow API", () => {
  beforeEach(() => {
    mocks.obtenerConversaciones.mockResolvedValue([reconocida, noRegistrada]);
    mocks.obtenerMensajes.mockImplementation(async (id: string) => mensajes[id]);
  });

  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it("muestra las conversaciones persistidas y los mensajes de la seleccionada", async () => {
    render(<ChatbotView />);

    expect(await screen.findByText("[SINTÉTICO] Quiero mi turno")).toBeTruthy();
    expect(screen.getByText("Aceptado por el proveedor")).toBeTruthy();
    expect(screen.getByText("Historial persistido en AgroFlow API")).toBeTruthy();
    expect(screen.getAllByText("Transportista Sintético").length).toBeGreaterThan(0);
    // La vista nunca afirma una entrega: sólo lo que informa la API.
    expect(screen.queryByText(/entregado/i)).toBeNull();
  });

  it("identifica al remitente no registrado y el envío incierto", async () => {
    render(<ChatbotView />);
    await screen.findByText("[SINTÉTICO] Quiero mi turno");

    fireEvent.click(screen.getByText("Remitente no registrado · +54•••••••0199", { selector: ".conv-top span" }));

    expect(await screen.findByText("[SINTÉTICO] No registrado")).toBeTruthy();
    expect(screen.getByText("Envío incierto · requiere revisión")).toBeTruthy();
    expect(screen.getByText("No registrado")).toBeTruthy();
    expect(screen.getAllByText("Requiere revisión").length).toBeGreaterThan(0);
    expect(mocks.obtenerMensajes).toHaveBeenCalledWith("c2");
  });

  describe("transcript", () => {
    const cuerpo = () => document.querySelector(".chat-transcript-body") as HTMLElement;
    const clicEn = (texto: string) => fireEvent.click(screen.getByText(texto, { selector: ".conv-top span" }));

    beforeEach(() => {
      vi.useFakeTimers({ toFake: ["Date"] });
      vi.setSystemTime(new Date("2026-11-06T21:00:00Z"));
      vi.spyOn(HTMLElement.prototype, "scrollHeight", "get").mockReturnValue(900);
    });

    afterEach(() => vi.useRealTimers());

    it("nombra el día real del último mensaje en el encabezado", async () => {
      mocks.obtenerConversaciones.mockResolvedValue([{ ...reconocida, ultimoMensajeUtc: "2026-11-05T15:00:00Z" }]);

      render(<ChatbotView />);
      await screen.findByText("[SINTÉTICO] Quiero mi turno");

      const encabezado = document.querySelector(".chat-transcript-head span")?.textContent;
      expect(encabezado).toContain("Ayer · 12:00");
      expect(encabezado).not.toContain("Hoy");
    });

    it("separa los mensajes por día", async () => {
      mocks.obtenerMensajes.mockResolvedValue([
        { id: "a", autor: "transportista", texto: "uno", hora: "12:00", fechaUtc: "2026-11-05T15:00:00Z" },
        { id: "b", autor: "bot", texto: "dos", hora: "12:01", fechaUtc: "2026-11-05T15:01:00Z" },
        { id: "c", autor: "transportista", texto: "tres", hora: "12:00", fechaUtc: "2026-11-06T15:00:00Z" },
      ]);

      render(<ChatbotView />);
      await screen.findByText("tres");

      const divisiones = Array.from(document.querySelectorAll(".chat-dia")).map((d) => d.textContent);
      expect(divisiones).toEqual(["Ayer", "Hoy"]);
    });

    it("va al último mensaje al abrir y en cada clic, aunque la conversación ya esté abierta", async () => {
      render(<ChatbotView />);
      await screen.findByText("[SINTÉTICO] Quiero mi turno");
      expect(cuerpo().scrollTop).toBe(900);

      cuerpo().scrollTop = 0;
      clicEn("Transportista Sintético");
      expect(cuerpo().scrollTop).toBe(900);

      cuerpo().scrollTop = 0;
      clicEn("Remitente no registrado · +54•••••••0199");
      await screen.findByText("[SINTÉTICO] No registrado");
      expect(cuerpo().scrollTop).toBe(900);
    });

    it("no muestra los mensajes de la conversación anterior mientras carga la nueva", async () => {
      render(<ChatbotView />);
      await screen.findByText("[SINTÉTICO] Quiero mi turno");
      mocks.obtenerMensajes.mockReturnValue(new Promise(() => undefined));

      clicEn("Remitente no registrado · +54•••••••0199");

      expect(screen.queryByText("[SINTÉTICO] Quiero mi turno")).toBeNull();
      expect(await screen.findByText("Cargando mensajes...")).toBeTruthy();
    });
  });

  it("muestra el estado vacío cuando todavía no hay conversaciones", async () => {
    mocks.obtenerConversaciones.mockResolvedValue([]);

    render(<ChatbotView />);

    expect(await screen.findByText("Todavía no hay conversaciones registradas.")).toBeTruthy();
  });

  it("conserva métricas y plantillas sin datos ni acciones, porque la API no las expone", async () => {
    render(<ChatbotView />);
    await screen.findByText("[SINTÉTICO] Quiero mi turno");

    expect(screen.getByText("Mensajes hoy").parentElement?.textContent).toContain("—");
    expect(screen.getByText("Sin plantillas.")).toBeTruthy();
    expect((screen.getByRole("button", { name: "Guardar plantillas" }) as HTMLButtonElement).disabled).toBe(true);
  });

  it("muestra el error accionable si falta la clave del dashboard", async () => {
    mocks.obtenerConversaciones.mockRejectedValue(new Error("Falta AGROFLOW_DASHBOARD_KEY"));

    render(<ChatbotView />);

    expect(await screen.findByText("Falta AGROFLOW_DASHBOARD_KEY")).toBeTruthy();
  });
});

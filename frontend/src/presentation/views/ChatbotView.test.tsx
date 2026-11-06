import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Conversacion, MensajeChat } from "../../domain/entities/Conversacion";

const mocks = vi.hoisted(() => ({
  obtenerConversaciones: vi.fn(),
  obtenerMensajes: vi.fn(),
}));

vi.mock("../../composition/container", () => ({
  container: {
    fuenteDatos: "http",
    apiUrl: "http://localhost:5000",
    conversaciones: {
      obtenerConversaciones: { execute: mocks.obtenerConversaciones },
      obtenerMensajesConversacion: { execute: mocks.obtenerMensajes },
      obtenerMetricasChatbot: {
        execute: async () => ({ mensajesHoy: 1, turnosAsignadosPorBot: 0, tiempoPromedioRespuestaSeg: 1, conversacionesEscaladas: 0 }),
      },
      obtenerFlujoDeAutomatizacion: { execute: async () => [] },
      obtenerPlantillas: { execute: async () => [] },
      guardarPlantillas: { execute: vi.fn() },
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

  it("muestra el estado vacío cuando todavía no hay conversaciones", async () => {
    mocks.obtenerConversaciones.mockResolvedValue([]);

    render(<ChatbotView />);

    expect(await screen.findByText("Todavía no hay conversaciones registradas.")).toBeTruthy();
  });

  it("muestra el error accionable si falta la clave del dashboard", async () => {
    mocks.obtenerConversaciones.mockRejectedValue(new Error("Falta AGROFLOW_DASHBOARD_KEY"));

    render(<ChatbotView />);

    expect(await screen.findByText("Falta AGROFLOW_DASHBOARD_KEY")).toBeTruthy();
  });
});

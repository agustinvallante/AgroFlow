import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Conversacion, MensajeChat } from "../../domain/entities/Conversacion";
import { ListadoIncompleto } from "../../domain/errors/ListadoIncompleto";

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

describe("ChatbotView con ZafraFlow API", () => {
  beforeEach(() => {
    mocks.obtenerConversaciones.mockReset().mockResolvedValue([reconocida, noRegistrada]);
    mocks.obtenerMensajes.mockReset().mockImplementation(async (id: string) => mensajes[id]);
  });

  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it("muestra las conversaciones persistidas y los mensajes de la seleccionada", async () => {
    render(<ChatbotView />);

    expect(await screen.findByText("[SINTÉTICO] Quiero mi turno")).toBeTruthy();
    expect(screen.getByRole("img", { name: "Aceptado por el proveedor" })).toBeTruthy();
    expect(screen.getByText("Historial persistido en ZafraFlow API")).toBeTruthy();
    expect(screen.getAllByText("Transportista Sintético").length).toBeGreaterThan(0);
    // La vista nunca afirma una entrega: sólo lo que informa la API.
    expect(screen.queryByText(/entregado/i)).toBeNull();
  });

  it("identifica al remitente no registrado y el envío incierto", async () => {
    render(<ChatbotView />);
    await screen.findByText("[SINTÉTICO] Quiero mi turno");

    fireEvent.click(screen.getByText("Remitente no registrado · +54•••••••0199", { selector: ".chat-conv-nombre" }));

    expect(await screen.findByText("[SINTÉTICO] No registrado")).toBeTruthy();
    expect(screen.getByRole("img", { name: "Envío incierto · requiere revisión" })).toBeTruthy();
    expect(screen.getByText("No registrado", { selector: ".chat-tag" })).toBeTruthy();
    expect(screen.getAllByText("Requiere revisión").length).toBeGreaterThan(0);
    expect(mocks.obtenerMensajes).toHaveBeenCalledWith("c2");
  });

  describe("transcript", () => {
    const cuerpo = () => document.querySelector(".chat-transcript-body") as HTMLElement;
    const clicEn = (texto: string) => fireEvent.click(screen.getByText(texto, { selector: ".chat-conv-nombre" }));

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

      const encabezado = document.querySelector(".chat-head-detalle")?.textContent;
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

    it("no mueve la vista cuando un refresco trae mensajes nuevos y el operador subió", async () => {
      render(<ChatbotView />);
      await screen.findByText("[SINTÉTICO] Quiero mi turno");
      cuerpo().scrollTop = 120;
      fireEvent.scroll(cuerpo());
      mocks.obtenerMensajes.mockResolvedValue([
        ...mensajes.c1,
        { id: "m9", autor: "transportista", texto: "[SINTÉTICO] Mensaje nuevo", hora: "12:05" },
      ]);

      fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));

      expect(await screen.findByText("[SINTÉTICO] Mensaje nuevo")).toBeTruthy();
      expect(cuerpo().scrollTop).toBe(120);
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

  describe("recuperación y respuestas fuera de orden", () => {
    const esperar = (ms = 0) => act(() => vi.advanceTimersByTimeAsync(ms));
    const seleccionar = (nombre: string) =>
      fireEvent.click(screen.getByText(nombre, { selector: ".chat-conv-nombre" }));

    beforeEach(() => vi.useFakeTimers());
    afterEach(() => vi.useRealTimers());

    it("se recupera por polling después de una falla inicial sin dejar el error bloqueando la vista", async () => {
      mocks.obtenerConversaciones.mockRejectedValueOnce(new Error("API no disponible."));
      render(<ChatbotView />);
      await esperar();
      expect(screen.getByRole("alert").textContent).toContain("API no disponible.");
      expect(screen.getByRole("button", { name: "Reintentar" })).toBeTruthy();
      expect(screen.queryByText("Todavía no hay conversaciones registradas.")).toBeNull();

      await esperar(3_000);
      expect(screen.getByText("[SINTÉTICO] Quiero mi turno")).toBeTruthy();
      expect(screen.queryByText("API no disponible.")).toBeNull();
      expect(screen.queryByRole("button", { name: "Reintentar" })).toBeNull();
    });

    it("permite reintentar la carga inicial, incluso si el primer reintento también falla", async () => {
      mocks.obtenerConversaciones
        .mockRejectedValueOnce(new Error("Primera falla."))
        .mockRejectedValueOnce(new Error("Segunda falla."));
      render(<ChatbotView />);
      await esperar();
      fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));
      await esperar();
      expect(screen.getByText("Segunda falla.")).toBeTruthy();
      fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));
      await esperar();
      expect(screen.getByText("[SINTÉTICO] Quiero mi turno")).toBeTruthy();
      expect(screen.queryByRole("alert")).toBeNull();
    });

    it("conserva historial y selección al fallar un refresco y limpia el aviso al recuperarse", async () => {
      render(<ChatbotView />);
      await esperar();
      seleccionar(noRegistrada.nombre);
      await esperar();
      mocks.obtenerConversaciones.mockRejectedValueOnce(new Error("Falló el refresco."));
      fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));
      await esperar();
      expect(screen.getByText("Datos desactualizados.")).toBeTruthy();
      expect(screen.getByText("[SINTÉTICO] No registrado")).toBeTruthy();
      expect(screen.queryByText("[SINTÉTICO] Quiero mi turno")).toBeNull();

      fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));
      await esperar();
      expect(screen.queryByText("Datos desactualizados.")).toBeNull();
      expect(screen.getByText("[SINTÉTICO] No registrado")).toBeTruthy();
    });

    it("una falla al refrescar mensajes conserva el historial de la misma conversación", async () => {
      render(<ChatbotView />);
      await esperar();
      mocks.obtenerMensajes.mockRejectedValueOnce(new Error("No se pudieron actualizar los mensajes."));
      fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));
      await esperar();
      expect(screen.getByText("No se pudieron actualizar los mensajes.")).toBeTruthy();
      expect(screen.getByText("[SINTÉTICO] Quiero mi turno")).toBeTruthy();
      fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));
      await esperar();
      expect(screen.queryByText("No se pudieron actualizar los mensajes.")).toBeNull();
    });

    it("una carga inicial pendiente no se superpone y su falla tardía no bloquea la recuperación", async () => {
      const inicial = diferida<Conversacion[]>();
      mocks.obtenerConversaciones.mockReturnValueOnce(inicial.promesa);
      render(<ChatbotView />);
      await esperar(9_000);
      expect(mocks.obtenerConversaciones).toHaveBeenCalledTimes(1);
      await act(async () => inicial.rechazar(new Error("Falla inicial atrasada.")));
      expect(screen.getByText("Falla inicial atrasada.")).toBeTruthy();
      await esperar(3_000);
      expect(screen.queryByText("Falla inicial atrasada.")).toBeNull();
      expect(screen.getByText("[SINTÉTICO] Quiero mi turno")).toBeTruthy();
    });

    it("un listado anterior no pisa la respuesta de un refresco más reciente", async () => {
      render(<ChatbotView />);
      await esperar();
      const anterior = diferida<Conversacion[]>();
      const reciente = diferida<Conversacion[]>();
      mocks.obtenerConversaciones.mockReturnValueOnce(anterior.promesa).mockReturnValueOnce(reciente.promesa);
      fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));
      fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));
      await act(async () => reciente.resolver([noRegistrada]));
      await act(async () => anterior.resolver([reconocida]));
      await esperar();
      expect(screen.getByText("[SINTÉTICO] No registrado")).toBeTruthy();
      expect(screen.queryByText("[SINTÉTICO] Quiero mi turno")).toBeNull();
    });

    it.each(["respuesta", "error"])("descarta una %s tardía de la conversación anterior", async (tipo) => {
      render(<ChatbotView />);
      await esperar();
      const anterior = diferida<MensajeChat[]>();
      mocks.obtenerMensajes.mockReturnValueOnce(anterior.promesa);
      seleccionar(noRegistrada.nombre);
      await esperar();
      seleccionar(reconocida.nombre);
      await esperar();
      await act(async () => {
        if (tipo === "respuesta") anterior.resolver(mensajes.c2);
        else anterior.rechazar(new Error("Error de la conversación anterior."));
      });
      expect(screen.getByText("[SINTÉTICO] Quiero mi turno")).toBeTruthy();
      expect(screen.queryByText("[SINTÉTICO] No registrado")).toBeNull();
      expect(screen.queryByText("Error de la conversación anterior.")).toBeNull();
    });

    it("retira la selección si deja de existir y no reutiliza sus mensajes en otra conversación", async () => {
      render(<ChatbotView />);
      await esperar();
      mocks.obtenerConversaciones.mockResolvedValueOnce([]);
      fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));
      await esperar();
      expect(screen.queryByText("[SINTÉTICO] Quiero mi turno")).toBeNull();
      expect(screen.getByText("Todavía no hay conversaciones registradas.")).toBeTruthy();
      mocks.obtenerConversaciones.mockResolvedValueOnce([noRegistrada]);
      fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));
      await esperar();
      expect(screen.getByText("[SINTÉTICO] No registrado")).toBeTruthy();
      expect(screen.queryByText("[SINTÉTICO] Quiero mi turno")).toBeNull();
    });
  });
  describe("en vivo", () => {
    const esperar = (ms = 0) => act(() => vi.advanceTimersByTimeAsync(ms));
    // React aplica los efectos al cerrar cada act: avanzar de a un segundo deja correr cada lectura.
    const segundos = async (n: number) => {
      for (let i = 0; i < n; i++) await esperar(1_000);
    };
    const cuerpo = () => document.querySelector(".chat-transcript-body") as HTMLElement;
    const clicEn = (nombre: string) => fireEvent.click(screen.getByText(nombre, { selector: ".chat-conv-nombre" }));
    const nombresVisibles = () => Array.from(document.querySelectorAll(".chat-conv-nombre")).map((n) => n.textContent);
    const nuevo: MensajeChat = { id: "m9", autor: "transportista", texto: "[SINTÉTICO] Se pinchó una goma", hora: "12:05" };
    const otroNuevo: MensajeChat = { id: "m10", autor: "transportista", texto: "[SINTÉTICO] Sigo esperando", hora: "12:06" };
    let alto = 900;
    let visibilidad: DocumentVisibilityState = "visible";
    const cambiarVisibilidad = (estado: DocumentVisibilityState) =>
      act(async () => {
        visibilidad = estado;
        document.dispatchEvent(new Event("visibilitychange"));
      });

    beforeEach(() => {
      vi.useFakeTimers();
      alto = 900;
      visibilidad = "visible";
      vi.spyOn(HTMLElement.prototype, "scrollHeight", "get").mockImplementation(() => alto);
      vi.spyOn(Document.prototype, "visibilityState", "get").mockImplementation(() => visibilidad);
    });
    afterEach(() => vi.useRealTimers());

    it("muestra un mensaje nuevo al segundo sin tocar Actualizar y refresca la lista cada 3 s", async () => {
      render(<ChatbotView />);
      await esperar();
      mocks.obtenerMensajes.mockResolvedValue([...mensajes.c1, nuevo]);

      await esperar(1_000);
      expect(screen.getByText("[SINTÉTICO] Se pinchó una goma")).toBeTruthy();
      expect(mocks.obtenerConversaciones).toHaveBeenCalledTimes(1);

      mocks.obtenerConversaciones.mockResolvedValue([{ ...reconocida, preview: "[SINTÉTICO] Se pinchó" }, noRegistrada]);
      await esperar(1_999);
      expect(mocks.obtenerConversaciones).toHaveBeenCalledTimes(1);
      await esperar(1);
      expect(mocks.obtenerConversaciones).toHaveBeenCalledTimes(2);
      expect(screen.getByText("[SINTÉTICO] Se pinchó")).toBeTruthy();
      // La selección se conserva.
      expect(screen.getByText("[SINTÉTICO] Quiero mi turno")).toBeTruthy();
      expect(mocks.obtenerMensajes).not.toHaveBeenCalledWith("c2");
    });

    it("no empieza una lectura mientras la anterior del mismo tipo sigue pendiente", async () => {
      render(<ChatbotView />);
      await esperar();
      const mensajesPendientes = diferida<MensajeChat[]>();
      const listaPendiente = diferida<Conversacion[]>();
      mocks.obtenerMensajes.mockReturnValue(mensajesPendientes.promesa);
      mocks.obtenerConversaciones.mockReturnValue(listaPendiente.promesa);

      await segundos(10);
      expect(mocks.obtenerMensajes).toHaveBeenCalledTimes(2);
      expect(mocks.obtenerConversaciones).toHaveBeenCalledTimes(2);

      await act(async () => {
        mensajesPendientes.resolver([...mensajes.c1, nuevo]);
        listaPendiente.resolver([reconocida, noRegistrada]);
      });
      expect(screen.getByText("[SINTÉTICO] Se pinchó una goma")).toBeTruthy();
      await segundos(3);
      expect(mocks.obtenerMensajes).toHaveBeenCalledTimes(5);
      expect(mocks.obtenerConversaciones).toHaveBeenCalledTimes(3);
    });

    it("pausa las lecturas con la pestaña oculta y lee de inmediato al volver", async () => {
      render(<ChatbotView />);
      await esperar();
      await cambiarVisibilidad("hidden");
      await segundos(10);
      expect(mocks.obtenerMensajes).toHaveBeenCalledTimes(1);
      expect(mocks.obtenerConversaciones).toHaveBeenCalledTimes(1);

      mocks.obtenerMensajes.mockResolvedValue([...mensajes.c1, nuevo]);
      await cambiarVisibilidad("visible");
      await esperar();
      expect(mocks.obtenerMensajes).toHaveBeenCalledTimes(2);
      expect(mocks.obtenerConversaciones).toHaveBeenCalledTimes(2);
      expect(screen.getByText("[SINTÉTICO] Se pinchó una goma")).toBeTruthy();
    });

    it("mantiene abajo al operador que está al final", async () => {
      render(<ChatbotView />);
      await esperar();
      expect(cuerpo().scrollTop).toBe(900);
      alto = 1_000;
      mocks.obtenerMensajes.mockResolvedValue([...mensajes.c1, nuevo]);

      await esperar(1_000);
      expect(cuerpo().scrollTop).toBe(1_000);
      expect(screen.queryByRole("button", { name: /mensajes? nuevos?/ })).toBeNull();
    });

    it("no mueve al operador que subió y le ofrece bajar a los mensajes nuevos", async () => {
      render(<ChatbotView />);
      await esperar();
      cuerpo().scrollTop = 100;
      fireEvent.scroll(cuerpo());
      alto = 1_000;
      mocks.obtenerMensajes.mockResolvedValue([...mensajes.c1, nuevo]);

      await esperar(1_000);
      expect(cuerpo().scrollTop).toBe(100);
      expect(screen.getByRole("button", { name: "1 mensaje nuevo" })).toBeTruthy();

      mocks.obtenerMensajes.mockResolvedValue([...mensajes.c1, nuevo, otroNuevo]);
      await esperar(1_000);
      fireEvent.click(screen.getByRole("button", { name: "2 mensajes nuevos" }));
      expect(cuerpo().scrollTop).toBe(1_000);
      expect(screen.queryByRole("button", { name: /mensajes? nuevos?/ })).toBeNull();
    });

    it("al seleccionar la conversación otra vez baja al último mensaje y descarta el aviso", async () => {
      render(<ChatbotView />);
      await esperar();
      cuerpo().scrollTop = 100;
      fireEvent.scroll(cuerpo());
      mocks.obtenerMensajes.mockResolvedValue([...mensajes.c1, nuevo]);
      await esperar(1_000);
      expect(screen.getByRole("button", { name: "1 mensaje nuevo" })).toBeTruthy();

      clicEn(reconocida.nombre);
      expect(cuerpo().scrollTop).toBe(900);
      expect(screen.queryByRole("button", { name: "1 mensaje nuevo" })).toBeNull();
    });

    it("anima sólo los mensajes que llegan con la conversación ya abierta", async () => {
      render(<ChatbotView />);
      await esperar();
      expect(document.querySelectorAll(".chat-msg-nuevo")).toHaveLength(0);
      mocks.obtenerMensajes.mockResolvedValue([...mensajes.c1, nuevo]);

      await esperar(1_000);
      const animados = Array.from(document.querySelectorAll(".chat-msg-nuevo"));
      expect(animados.map((m) => m.querySelector(".chat-msg-texto")?.textContent)).toEqual(["[SINTÉTICO] Se pinchó una goma"]);

      clicEn(noRegistrada.nombre);
      await esperar();
      expect(document.querySelectorAll(".chat-msg-nuevo")).toHaveLength(0);
    });

    it("indica que el bot está respondiendo mientras el último mensaje entrante sigue en proceso", async () => {
      const entrante: MensajeChat = { ...nuevo, procesamiento: "en-curso" };
      mocks.obtenerMensajes.mockResolvedValue([...mensajes.c1, entrante]);
      render(<ChatbotView />);
      await esperar();
      expect(screen.getByRole("status").textContent).toBe("El bot está respondiendo");

      mocks.obtenerMensajes.mockResolvedValue([
        ...mensajes.c1,
        { ...entrante, procesamiento: "completado" },
        { id: "m11", autor: "bot", texto: "[SINTÉTICO] Lamento el inconveniente", hora: "12:05", estadoEnvio: "pendiente" },
      ]);
      await esperar(1_000);
      expect(screen.getByRole("status").textContent).toBe("");
    });

    it("no indica respuesta en curso ante un procesamiento fallido y conserva su etiqueta", async () => {
      mocks.obtenerMensajes.mockResolvedValue([...mensajes.c1, { ...nuevo, procesamiento: "fallido" }]);
      render(<ChatbotView />);
      await esperar();
      expect(screen.getByRole("status").textContent).toBe("");
      expect(screen.getByText("Sin respuesta · procesamiento fallido")).toBeTruthy();
    });

    it("cuenta las novedades de otra conversación, la destaca y las borra al abrirla", async () => {
      render(<ChatbotView />);
      await esperar();
      expect(screen.queryByLabelText(/sin leer/)).toBeNull();
      const conActividad = { ...noRegistrada, preview: "[SINTÉTICO] Otra vez", ultimoMensajeUtc: "2026-11-06T21:00:00Z" };
      mocks.obtenerConversaciones.mockResolvedValue([reconocida, conActividad]);

      await esperar(3_000);
      expect(screen.getByLabelText("1 novedad sin leer").textContent).toBe("1");
      const fila = screen.getByText(noRegistrada.nombre, { selector: ".chat-conv-nombre" }).closest("button")!;
      expect(fila.className).toContain("destacada");

      mocks.obtenerConversaciones.mockResolvedValue([reconocida, { ...conActividad, ultimoMensajeUtc: "2026-11-06T21:01:00Z" }]);
      await esperar(3_000);
      expect(screen.getByLabelText("2 novedades sin leer").textContent).toBe("2");

      clicEn(noRegistrada.nombre);
      await esperar();
      expect(screen.queryByLabelText(/sin leer/)).toBeNull();
      await esperar(3_000);
      expect(screen.queryByLabelText(/sin leer/)).toBeNull();
      // Conserva el orden de la API.
      expect(nombresVisibles()).toEqual([reconocida.nombre, noRegistrada.nombre]);
    });

    it("busca por nombre o mensaje y filtra por estado", async () => {
      render(<ChatbotView />);
      await esperar();
      const buscar = screen.getByLabelText("Buscar conversación");

      fireEvent.change(buscar, { target: { value: "transportista" } });
      expect(nombresVisibles()).toEqual([reconocida.nombre]);
      fireEvent.change(buscar, { target: { value: "HOLA" } });
      expect(nombresVisibles()).toEqual([noRegistrada.nombre]);
      fireEvent.change(buscar, { target: { value: "sintetico] respuesta" } });
      expect(nombresVisibles()).toEqual([reconocida.nombre]);
      fireEvent.change(buscar, { target: { value: "zzz" } });
      expect(nombresVisibles()).toEqual([]);
      expect(screen.getByText("Ninguna conversación coincide con la búsqueda.")).toBeTruthy();
      fireEvent.change(buscar, { target: { value: "" } });

      const filtrar = (nombre: string) => fireEvent.click(screen.getByRole("button", { name: nombre }));
      filtrar("Con el bot");
      expect(nombresVisibles()).toEqual([reconocida.nombre]);
      expect(screen.getByRole("button", { name: "Con el bot" }).getAttribute("aria-pressed")).toBe("true");
      filtrar("Requiere revisión");
      expect(nombresVisibles()).toEqual([noRegistrada.nombre]);
      filtrar("No registrado");
      expect(nombresVisibles()).toEqual([noRegistrada.nombre]);
      filtrar("Todas");
      expect(nombresVisibles()).toEqual([reconocida.nombre, noRegistrada.nombre]);
    });

    it("nombra cada estado de envío en su ícono", async () => {
      mocks.obtenerMensajes.mockResolvedValue([
        { id: "e1", autor: "bot", texto: "uno", hora: "12:00", estadoEnvio: "pendiente" },
        { id: "e2", autor: "bot", texto: "dos", hora: "12:00", estadoEnvio: "aceptado" },
        { id: "e3", autor: "bot", texto: "tres", hora: "12:00", estadoEnvio: "fallido" },
        { id: "e4", autor: "bot", texto: "Turno asignado.\nFinca Norte", hora: "12:00", estadoEnvio: "incierto" },
      ]);
      render(<ChatbotView />);
      await esperar();

      const estados = ["Pendiente de envío", "Aceptado por el proveedor", "Envío fallido", "Envío incierto · requiere revisión"];
      for (const nombre of estados) {
        expect(screen.getByRole("img", { name: nombre }).querySelector("title")?.textContent).toBe(nombre);
      }
      expect(screen.getAllByText("Bot ZafraFlow")).toHaveLength(4);
      const textos = Array.from(document.querySelectorAll(".chat-msg-texto")).map((t) => t.textContent);
      expect(textos).toContain("Turno asignado.\nFinca Norte");
    });

    it("muestra el listado incompleto con su aviso y lo limpia cuando un refresco lo completa", async () => {
      mocks.obtenerConversaciones.mockRejectedValueOnce(new ListadoIncompleto([reconocida], "Listado incompleto de prueba."));
      render(<ChatbotView />);
      await esperar();
      expect(screen.getByText("Listado incompleto de prueba.")).toBeTruthy();
      expect(screen.getByText("[SINTÉTICO] Quiero mi turno")).toBeTruthy();

      await esperar(3_000);
      expect(screen.queryByText("Listado incompleto de prueba.")).toBeNull();
      expect(nombresVisibles()).toEqual([reconocida.nombre, noRegistrada.nombre]);
    });

    it("un refresco periódico fallido conserva los datos y deja de indicar En vivo hasta recuperarse", async () => {
      render(<ChatbotView />);
      await esperar();
      expect(screen.getByText("En vivo")).toBeTruthy();
      mocks.obtenerConversaciones.mockRejectedValueOnce(new Error("Falló el refresco."));

      await esperar(3_000);
      expect(screen.getByText("Datos desactualizados.")).toBeTruthy();
      expect(screen.getByText("Sin actualizar")).toBeTruthy();
      expect(screen.getByText("[SINTÉTICO] Quiero mi turno")).toBeTruthy();

      await esperar(3_000);
      expect(screen.queryByText("Datos desactualizados.")).toBeNull();
      expect(screen.getByText("En vivo")).toBeTruthy();
    });

    it("muestra avatar, dirección enmascarada y que la respuesta manual no está disponible", async () => {
      render(<ChatbotView />);
      await esperar();
      expect(document.querySelector(".chat-head .chat-avatar")?.textContent).toBe("TS");
      expect(document.querySelector(".chat-head-detalle")?.textContent).toContain("+54•••••••0155");
      expect(screen.getByText(/La respuesta manual del operador todavía no está disponible/)).toBeTruthy();
      const avatarNoRegistrado = screen.getByText(noRegistrada.nombre, { selector: ".chat-conv-nombre" })
        .closest("button")!.querySelector(".chat-avatar")!;
      expect(avatarNoRegistrado.textContent).toBe("?");
      expect(avatarNoRegistrado.className).toContain("no-registrado");
    });
  });
});

function diferida<T>() {
  let resolver!: (valor: T) => void;
  let rechazar!: (error: unknown) => void;
  const promesa = new Promise<T>((res, rej) => { resolver = res; rechazar = rej; });
  return { promesa, resolver, rechazar };
}

import { act, cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({ fincas: vi.fn(), turnos: vi.fn() }));

vi.mock("../../composition/container", () => ({
  container: {
    fincas: { obtenerFincas: { execute: mocks.fincas } },
    turnos: { obtenerTurnosActivos: { execute: mocks.turnos } },
  },
}));

import { MapaView } from "./MapaView";

/** El catálogo que publica la API en LocalDemo. */
const FINCAS = [
  { id: "f1", codigo: "FINCA-NORTE", nombre: "Finca Norte" },
  { id: "f2", codigo: "FINCA-SUR", nombre: "Finca Sur" },
];

const escribir = (etiqueta: string, valor: string) =>
  fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valor } });
const clic = (nombre: string) => fireEvent.click(screen.getByRole("button", { name: nombre }));
const contador = (texto: RegExp) => document.querySelector(".mapa-contadores")?.textContent?.match(texto)?.[1];
const camiones = () => document.querySelectorAll(".mapa-camion circle");
const avanzar = (ms: number) =>
  act(() => {
    vi.advanceTimersByTime(ms);
  });
const MINUTO = 60_000;

/** Monta la vista y espera las fincas de la API. */
async function montar() {
  const vista = render(<MapaView />);
  await act(async () => {});
  return vista;
}

function cargarViaje(patente = "af 600 ff") {
  escribir("Patente", patente);
  clic("Agregar viaje");
}

describe("MapaView", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-11-08T15:00:00Z"));
    mocks.fincas.mockReset().mockResolvedValue(FINCAS);
    mocks.turnos.mockReset().mockResolvedValue([]);
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it("empieza sin viajes, con el ingenio de referencia y las fincas de la API", async () => {
    await montar();

    const mapa = within(screen.getByRole("img"));
    expect(mapa.getByText("Finca Norte")).toBeTruthy();
    expect(mapa.getByText("Finca Sur")).toBeTruthy();
    expect(mapa.getByText("INGENIO")).toBeTruthy();
    expect(screen.getByText("Sin viajes cargados.")).toBeTruthy();
    expect(screen.getByText("Tiempo real")).toBeTruthy();
  });

  it("si la API no publica fincas lo avisa y permite reintentar", async () => {
    mocks.fincas.mockReset().mockRejectedValueOnce(new Error("API no disponible.")).mockResolvedValue(FINCAS);
    await montar();

    expect(screen.getByRole("alert").textContent).toContain("API no disponible.");
    clic("Reintentar");
    await act(async () => {});

    expect(screen.queryByRole("alert")).toBeNull();
    expect(within(screen.getByRole("img")).getByText("Finca Norte")).toBeTruthy();
  });

  it("agrega solo el viaje de un turno de la cola, lo acelera y no permite quitarlo", async () => {
    mocks.turnos.mockResolvedValue([
      {
        id: "t1",
        ventana: { inicio: "2026-11-08T16:30:00Z", fin: "2026-11-08T17:00:00Z" },
        hora: "13:30",
        offsetMin: 90,
        patente: "AF123BC",
        chofer: "Juan Pérez",
        finca: "Finca Sur",
        horasDesdeCorte: 1,
        estado: "pendiente",
      },
    ]);
    await montar();

    expect(screen.getByText("AF123BC", { selector: "strong" })).toBeTruthy();
    // La fila no indica si el viaje viene de la cola o de la demo.
    expect(screen.getByText(/^Finca Sur · \d+ km$/)).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Quitar viaje AF123BC" })).toBeNull();
    expect(contador(/Programados (\d+)/)).toBe("1");

    // A x60 el turno también avanza: 90 minutos simulados en 90 segundos.
    clic("x60");
    avanzar(75_000);
    clic("Ver camión AF123BC");
    expect(screen.getByText("Juan Pérez")).toBeTruthy();
    avanzar(15_000);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
  });

  it("muestra el viaje cargado en el mapa y la cola", async () => {
    await montar();
    cargarViaje();

    expect(screen.getByText("AF600FF", { selector: "strong" })).toBeTruthy();
    expect(contador(/En ruta (\d+)/)).toBe("1");
    expect(camiones().length).toBe(1);
  });

  it("al tocar un camión muestra su detalle y lo cierra", async () => {
    await montar();
    escribir("Finca de origen", "f2");
    cargarViaje();
    avanzar(5 * MINUTO);

    clic("Ver camión AF600FF");
    const detalle = within(screen.getByRole("dialog", { name: "Detalle del camión AF600FF" }));
    expect(detalle.getByText("En ruta")).toBeTruthy();
    expect(detalle.getByText("Finca Sur")).toBeTruthy();
    for (const dato of ["Salida", "Llegada", "Distancia", "Restante"]) expect(detalle.getByText(dato)).toBeTruthy();

    clic("Cerrar detalle");
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("oculta la etiqueta que no entra y la deja disponible al pasar el cursor", async () => {
    await montar();
    for (let i = 0; i < 6; i++) cargarViaje(`AF00${i}XX`);

    const visibles = document.querySelectorAll(".mapa-camion text").length;
    const ocultas = document.querySelectorAll(".mapa-camion title").length;

    expect(visibles).toBeGreaterThan(0);
    expect(ocultas).toBeGreaterThan(0);
    expect(visibles + ocultas).toBe(6);
  });

  it("hace un solo recorrido: llega, lo muestra y no vuelve a empezar", async () => {
    await montar();
    cargarViaje();
    clic("x60");
    const inicial = camiones()[0].getAttribute("cx");

    avanzar(1000);
    expect(camiones()[0].getAttribute("cx")).not.toBe(inicial);

    // Ninguna finca queda a más de una hora a 40 km/h; a x60, menos de un minuto real.
    avanzar(90_000);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
    expect(camiones().length).toBe(0);
    expect(within(screen.getByRole("img")).getByText(/AF600FF · LLEGÓ/)).toBeTruthy();

    avanzar(600_000);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
    expect(contador(/En ruta (\d+)/)).toBe("0");
  });

  it("a x1 avanza con el reloj real", async () => {
    await montar();
    cargarViaje();

    avanzar(MINUTO);
    expect(contador(/En ruta (\d+)/)).toBe("1");

    avanzar(90 * MINUTO);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
  });

  it("marca demora si tarda más que lo estimado y luego llega", async () => {
    await montar();
    escribir("Retraso simulado (min)", "600");
    cargarViaje();

    avanzar(2 * 60 * MINUTO);
    expect(contador(/Demora (\d+)/)).toBe("1");

    avanzar(10 * 60 * MINUTO);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
    expect(contador(/Demora (\d+)/)).toBe("0");
  });

  it("conserva lo cargado al volver a la pestaña y sigue por donde iba", async () => {
    const { unmount } = await montar();
    cargarViaje();
    unmount();

    vi.setSystemTime(new Date("2026-11-08T17:00:00Z"));
    await montar();

    expect(screen.getByText("AF600FF", { selector: "strong" })).toBeTruthy();
    expect(contador(/Llegaron (\d+)/)).toBe("1");
  });

  it("rechaza datos inválidos y un camión con viaje sin terminar", async () => {
    await montar();

    escribir("Velocidad (km/h)", "0");
    cargarViaje();
    expect(screen.getByText("La velocidad debe estar entre 1 y 120 km/h.")).toBeTruthy();

    escribir("Velocidad (km/h)", "40");
    cargarViaje();
    cargarViaje("AF600FF");
    expect(screen.getByText("Ese camión ya tiene un viaje sin terminar.")).toBeTruthy();
  });

  it("rechaza una ubicación del ingenio fuera de rango", async () => {
    await montar();

    escribir("Nombre del ingenio", "Lejano");
    escribir("Latitud del ingenio", "91");
    escribir("Longitud del ingenio", "-65");
    clic("Cambiar ubicación");

    expect(screen.getByText("La latitud debe estar entre -90 y 90.")).toBeTruthy();
    expect(within(screen.getByRole("img")).queryByText("LEJANO")).toBeNull();
  });

  it.each([
    ["Velocidad (km/h)", "1e-12", "La velocidad debe estar entre 1 y 120 km/h."],
    ["Velocidad (km/h)", "Infinity", "La velocidad debe estar entre 1 y 120 km/h."],
    ["Velocidad (km/h)", "121", "La velocidad debe estar entre 1 y 120 km/h."],
    ["Sale en (min)", "-1", "La salida debe estar entre 0 y 1440 minutos."],
    ["Sale en (min)", "1e12", "La salida debe estar entre 0 y 1440 minutos."],
    ["Retraso simulado (min)", "-5", "El retraso debe estar entre 0 y 1440 minutos."],
    ["Retraso simulado (min)", "Infinity", "El retraso debe estar entre 0 y 1440 minutos."],
  ])("rechaza %s = %s sin agregar el viaje", async (campo, valor, error) => {
    await montar();

    escribir(campo, valor);
    cargarViaje();

    expect(screen.getByText(error)).toBeTruthy();
    expect(screen.getByText("Sin viajes cargados.")).toBeTruthy();
  });

  it("muestra Programado hasta la hora de salida", async () => {
    await montar();
    escribir("Sale en (min)", "30");
    cargarViaje();

    expect(contador(/Programados (\d+)/)).toBe("1");
    expect(screen.getByText("Programado", { selector: "em" })).toBeTruthy();

    avanzar(31 * MINUTO);
    expect(contador(/Programados (\d+)/)).toBe("0");
    expect(contador(/En ruta (\d+)/)).toBe("1");
  });

  it("conserva el adelanto acumulado al recargar", async () => {
    const { unmount } = await montar();
    clic("x60");
    avanzar(10_000);
    unmount();

    await montar();

    // 10 s a x60 adelantan 590 s: unos 10 minutos.
    expect(screen.getByText("Adelantado 10 min")).toBeTruthy();
    expect(screen.getByRole("button", { name: "x1", pressed: true })).toBeTruthy();
  });

  it("a x60 no escribe en el navegador en cada tick", async () => {
    await montar();
    clic("x60");
    const escrituras = vi.spyOn(Storage.prototype, "setItem");

    avanzar(20_000);

    // 200 ticks; el adelanto se guarda cada 5 s.
    expect(escrituras.mock.calls.length).toBeLessThanOrEqual(5);
  });

  it("sigue funcionando si el navegador no permite guardar", async () => {
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
      throw new Error("bloqueado");
    });
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
      throw new Error("bloqueado");
    });
    await montar();
    cargarViaje();

    expect(screen.getByText("AF600FF", { selector: "strong" })).toBeTruthy();
    expect(contador(/En ruta (\d+)/)).toBe("1");
  });

  it("descarta lo guardado que no es válido en lugar de romper la vista", async () => {
    const viaje = { fincaId: "f1", salidaMs: Date.now(), velocidadKmh: 40, retrasoMin: 0 };
    localStorage.setItem(
      "agroflow.mapa-simulado.v3",
      JSON.stringify({
        ingenio: { nombre: "Sin coordenadas" },
        viajes: [
          { ...viaje, id: "v1", patente: "AF100AA" },
          { ...viaje, id: "v2", patente: "AF200BB", velocidadKmh: 1e-12 },
          { ...viaje, id: "v3", patente: "AF300CC", salidaMs: 1e300 },
        ],
        siguienteId: 4,
        adelantoMs: Number.MAX_VALUE,
      }),
    );

    await montar();

    expect(screen.getByText("AF100AA", { selector: "strong" })).toBeTruthy();
    for (const patente of ["AF200BB", "AF300CC"]) expect(screen.queryByText(patente, { selector: "strong" })).toBeNull();
    expect(within(screen.getByRole("img")).getByText("INGENIO")).toBeTruthy();
    expect(screen.getByText("Tiempo real")).toBeTruthy();
  });
});

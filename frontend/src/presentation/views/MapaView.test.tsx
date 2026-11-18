import { act, cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { MapaView } from "./MapaView";

const escribir = (etiqueta: string, valor: string) =>
  fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valor } });
const clic = (nombre: string) => fireEvent.click(screen.getByRole("button", { name: nombre }));
const contador = (texto: RegExp) => document.querySelector(".mapa-contadores")?.textContent?.match(texto)?.[1];
const camiones = () => document.querySelectorAll(".mapa-camion rect");
const avanzar = (ms: number) =>
  act(() => {
    vi.advanceTimersByTime(ms);
  });

function cargarIngenio() {
  escribir("Nombre del ingenio", "Ingenio Prueba");
  escribir("Latitud del ingenio", "-26.75");
  escribir("Longitud del ingenio", "-65");
  clic("Guardar ingenio");
}

function cargarFinca(nombre = "Finca Prueba") {
  escribir("Nombre de la finca", nombre);
  escribir("Latitud de la finca", "-26,6");
  escribir("Longitud de la finca", "-65.4");
  clic("Agregar finca");
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
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
  });

  it("empieza vacío, sin datos precargados", () => {
    render(<MapaView />);

    expect(screen.getByText(/Cargá el ingenio/)).toBeTruthy();
    expect(screen.getByText("Sin viajes cargados.")).toBeTruthy();
    expect(screen.getByText("Tiempo real")).toBeTruthy();
    expect(contador(/En ruta (\d+)/)).toBe("0");
  });

  it("exige el ingenio y una finca antes del viaje", () => {
    render(<MapaView />);

    cargarViaje();
    expect(screen.getByText("Primero guardá el ingenio.")).toBeTruthy();

    cargarIngenio();
    cargarViaje();
    expect(screen.getByText("Primero agregá una finca.")).toBeTruthy();
  });

  it("muestra el viaje cargado en el mapa y la cola", () => {
    render(<MapaView />);
    cargarIngenio();
    cargarFinca();
    cargarViaje();

    expect(within(screen.getByRole("img")).getByText("Finca Prueba")).toBeTruthy();
    expect(screen.getByText("AF600FF", { selector: "strong" })).toBeTruthy();
    expect(contador(/En ruta (\d+)/)).toBe("1");
    expect(camiones().length).toBe(1);
  });

  it("oculta la etiqueta que no entra y la deja disponible al pasar el cursor", () => {
    render(<MapaView />);
    cargarIngenio();
    cargarFinca();
    for (let i = 0; i < 6; i++) cargarViaje(`AF00${i}XX`);

    const visibles = document.querySelectorAll(".mapa-camion text").length;
    const ocultas = document.querySelectorAll(".mapa-camion title").length;

    expect(visibles).toBeGreaterThan(0);
    expect(ocultas).toBeGreaterThan(0);
    expect(visibles + ocultas).toBe(6);
  });

  it("hace un solo recorrido: llega, lo muestra y no vuelve a empezar", () => {
    render(<MapaView />);
    cargarIngenio();
    cargarFinca();
    cargarViaje();
    clic("x60");
    const inicial = camiones()[0].getAttribute("x");

    avanzar(1000);
    expect(camiones()[0].getAttribute("x")).not.toBe(inicial);

    // 56 km a 40 km/h son 84 minutos; a x60, 84 segundos.
    avanzar(90_000);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
    expect(camiones().length).toBe(0);
    expect(within(screen.getByRole("img")).getByText(/AF600FF · LLEGÓ/)).toBeTruthy();

    avanzar(600_000);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
    expect(contador(/En ruta (\d+)/)).toBe("0");
  });

  it("a x1 avanza con el reloj real", () => {
    render(<MapaView />);
    cargarIngenio();
    cargarFinca();
    cargarViaje();

    avanzar(83 * 60_000);
    expect(contador(/En ruta (\d+)/)).toBe("1");

    avanzar(2 * 60_000);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
  });

  it("marca demora si tarda más que lo estimado y luego llega", () => {
    render(<MapaView />);
    cargarIngenio();
    cargarFinca();
    escribir("Retraso simulado (min)", "20");
    cargarViaje();

    avanzar(90 * 60_000);
    expect(contador(/Demora (\d+)/)).toBe("1");

    avanzar(20 * 60_000);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
    expect(contador(/Demora (\d+)/)).toBe("0");
  });

  it("conserva lo cargado al volver a la pestaña y sigue por donde iba", () => {
    const { unmount } = render(<MapaView />);
    cargarIngenio();
    cargarFinca();
    cargarViaje();
    unmount();

    vi.setSystemTime(new Date("2026-11-08T17:00:00Z"));
    render(<MapaView />);

    expect(screen.getByText("AF600FF", { selector: "strong" })).toBeTruthy();
    expect(contador(/Llegaron (\d+)/)).toBe("1");
  });

  it("rechaza datos inválidos y un camión con viaje sin terminar", () => {
    render(<MapaView />);
    cargarIngenio();
    cargarFinca();

    cargarFinca("finca prueba");
    expect(screen.getByText("Ya existe una finca con ese nombre.")).toBeTruthy();

    escribir("Nombre de la finca", "Otra");
    escribir("Latitud de la finca", "abc");
    clic("Agregar finca");
    expect(screen.getByText("La latitud debe estar entre -90 y 90.")).toBeTruthy();

    escribir("Velocidad (km/h)", "0");
    cargarViaje();
    expect(screen.getByText("La velocidad debe estar entre 1 y 120 km/h.")).toBeTruthy();

    escribir("Velocidad (km/h)", "40");
    cargarViaje();
    cargarViaje("AF600FF");
    expect(screen.getByText("Ese camión ya tiene un viaje sin terminar.")).toBeTruthy();
  });

  it("rechaza coordenadas fuera de rango", () => {
    render(<MapaView />);
    cargarIngenio();

    escribir("Nombre de la finca", "Lejana");
    escribir("Latitud de la finca", "91");
    escribir("Longitud de la finca", "-65");
    clic("Agregar finca");
    expect(screen.getByText("La latitud debe estar entre -90 y 90.")).toBeTruthy();

    escribir("Latitud de la finca", "-26");
    escribir("Longitud de la finca", "-181");
    clic("Agregar finca");
    expect(screen.getByText("La longitud debe estar entre -180 y 180.")).toBeTruthy();
    expect(within(screen.getByRole("img")).queryByText("Lejana")).toBeNull();
  });

  it.each([
    ["Velocidad (km/h)", "1e-12", "La velocidad debe estar entre 1 y 120 km/h."],
    ["Velocidad (km/h)", "Infinity", "La velocidad debe estar entre 1 y 120 km/h."],
    ["Velocidad (km/h)", "121", "La velocidad debe estar entre 1 y 120 km/h."],
    ["Sale en (min)", "-1", "La salida debe estar entre 0 y 1440 minutos."],
    ["Sale en (min)", "1e12", "La salida debe estar entre 0 y 1440 minutos."],
    ["Retraso simulado (min)", "-5", "El retraso debe estar entre 0 y 1440 minutos."],
    ["Retraso simulado (min)", "Infinity", "El retraso debe estar entre 0 y 1440 minutos."],
  ])("rechaza %s = %s sin agregar el viaje", (campo, valor, error) => {
    render(<MapaView />);
    cargarIngenio();
    cargarFinca();

    escribir(campo, valor);
    cargarViaje();

    expect(screen.getByText(error)).toBeTruthy();
    expect(screen.getByText("Sin viajes cargados.")).toBeTruthy();
  });

  it("muestra Programado hasta la hora de salida", () => {
    render(<MapaView />);
    cargarIngenio();
    cargarFinca();
    escribir("Sale en (min)", "30");
    cargarViaje();

    expect(contador(/Programados (\d+)/)).toBe("1");
    expect(screen.getByText("Programado", { selector: "em" })).toBeTruthy();

    avanzar(31 * 60_000);
    expect(contador(/Programados (\d+)/)).toBe("0");
    expect(contador(/En ruta (\d+)/)).toBe("1");
  });

  it("conserva el adelanto acumulado al recargar", () => {
    const { unmount } = render(<MapaView />);
    clic("x60");
    avanzar(10_000);
    unmount();

    render(<MapaView />);

    // 10 s a x60 adelantan 590 s: unos 10 minutos.
    expect(screen.getByText("Adelantado 10 min")).toBeTruthy();
    expect(screen.getByRole("button", { name: "x1", pressed: true })).toBeTruthy();
  });

  it("a x60 no escribe en el navegador en cada tick", () => {
    render(<MapaView />);
    clic("x60");
    const escrituras = vi.spyOn(Storage.prototype, "setItem");

    avanzar(20_000);

    // 200 ticks; el adelanto se guarda cada 5 s.
    expect(escrituras.mock.calls.length).toBeLessThanOrEqual(5);
    escrituras.mockRestore();
  });

  it("sigue funcionando si el navegador no permite guardar", () => {
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
      throw new Error("bloqueado");
    });
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
      throw new Error("bloqueado");
    });
    try {
      render(<MapaView />);
      cargarIngenio();
      cargarFinca();
      cargarViaje();

      expect(screen.getByText("AF600FF", { selector: "strong" })).toBeTruthy();
      expect(contador(/En ruta (\d+)/)).toBe("1");
    } finally {
      vi.restoreAllMocks();
    }
  });

  it("descarta lo guardado que no es válido en lugar de romper la vista", () => {
    const ingenio = { nombre: "Ingenio Prueba", lat: -26.75, lon: -65 };
    const finca = { id: "f1", nombre: "Finca Prueba", lat: -26.6, lon: -65.4 };
    const viaje = { fincaId: "f1", salidaMs: Date.now(), velocidadKmh: 40, retrasoMin: 0 };
    localStorage.setItem(
      "agroflow.mapa-simulado.v2",
      JSON.stringify({
        ingenio,
        fincas: [finca, { id: "f2", nombre: "Sin coordenadas" }],
        viajes: [
          { ...viaje, id: "v1", patente: "AF100AA" },
          { ...viaje, id: "v2", patente: "AF200BB", velocidadKmh: 1e-12 },
          { ...viaje, id: "v3", patente: "AF300CC", salidaMs: 1e300 },
          { ...viaje, id: "v4", patente: "AF400DD", fincaId: "f2" },
        ],
        siguienteId: 5,
        adelantoMs: Number.MAX_VALUE,
      }),
    );

    render(<MapaView />);

    expect(screen.getByText("AF100AA", { selector: "strong" })).toBeTruthy();
    for (const patente of ["AF200BB", "AF300CC", "AF400DD"])
      expect(screen.queryByText(patente, { selector: "strong" })).toBeNull();
    expect(within(screen.getByRole("img")).queryByText("Sin coordenadas")).toBeNull();
    expect(screen.getByText("Tiempo real")).toBeTruthy();
  });

  it("al quitar una finca quita también sus viajes", () => {
    render(<MapaView />);
    cargarIngenio();
    cargarFinca();
    cargarViaje();

    clic("Quitar Finca Prueba");

    expect(screen.queryByText("AF600FF", { selector: "strong" })).toBeNull();
  });
});

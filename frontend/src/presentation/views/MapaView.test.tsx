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
    expect(screen.getByText("La velocidad debe ser mayor a 0.")).toBeTruthy();

    escribir("Velocidad (km/h)", "40");
    cargarViaje();
    cargarViaje("AF600FF");
    expect(screen.getByText("Ese camión ya tiene un viaje sin terminar.")).toBeTruthy();
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

import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Turno } from "../../domain/entities/Turno";

const mocks = vi.hoisted(() => ({
  obtenerTurnos: vi.fn(),
  cambiarEstado: vi.fn(),
}));

vi.mock("../../composition/container", () => ({
  container: {
    fuenteDatos: "http",
    apiUrl: "http://localhost:5000",
    turnos: {
      obtenerTurnosDelDia: { execute: mocks.obtenerTurnos },
      obtenerDetalleTurno: { execute: vi.fn() },
      crearTurnoManual: { execute: vi.fn() },
      cambiarEstadoTurno: { execute: mocks.cambiarEstado },
      cancelarTurno: { execute: vi.fn() },
      reasignarHorarioTurno: { execute: vi.fn() },
    },
  },
}));

import { ColaTurnosView } from "./ColaTurnosView";
import { INTERVALO_POLLING_MS } from "../hooks/useColaTurnos";

const turno: Turno = {
  id: "t1",
  hora: "08:00",
  offsetMin: 0,
  patente: "AF123BC",
  chofer: "Juan Pérez",
  finca: "Finca Norte",
  flota: null,
  horasDesdeCorte: 3,
  estado: "pendiente",
  canal: null,
  esperaMin: null,
  prioridad: null,
  cargaTon: 28.5,
};

const SIN_COINCIDENCIAS = "No hay turnos que coincidan con estos filtros.";
const esperar = (ms = 0) => act(() => vi.advanceTimersByTimeAsync(ms));

describe("ColaTurnosView", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    mocks.obtenerTurnos.mockReset();
    mocks.cambiarEstado.mockReset();
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it("si falla la primera carga muestra el error y no el estado vacío", async () => {
    mocks.obtenerTurnos.mockRejectedValue(new Error("No se pudo conectar con la API."));

    render(<ColaTurnosView />);
    await esperar();

    expect(screen.getByText("No se pudo conectar con la API.")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Reintentar" })).toBeTruthy();
    expect(screen.queryByText(SIN_COINCIDENCIAS)).toBeNull();
    expect(screen.queryByRole("table")).toBeNull();
  });

  it("una respuesta 200 [] muestra el estado vacío sin error", async () => {
    mocks.obtenerTurnos.mockResolvedValue([]);

    render(<ColaTurnosView />);
    await esperar();

    expect(screen.getByText(SIN_COINCIDENCIAS)).toBeTruthy();
    expect(screen.queryByRole("alert")).toBeNull();
  });

  it("si falla el polling conserva la lista y la marca como desactualizada", async () => {
    mocks.obtenerTurnos.mockResolvedValueOnce([turno]).mockRejectedValue(new Error("Error interno de la API."));

    render(<ColaTurnosView />);
    await esperar();
    expect(screen.queryByText(/Datos desactualizados/)).toBeNull();

    await esperar(INTERVALO_POLLING_MS);

    expect(screen.getByText("AF123BC")).toBeTruthy();
    expect(screen.getByText(/Datos desactualizados/)).toBeTruthy();
    expect(screen.getByText(/Error interno de la API\./)).toBeTruthy();
  });

  it("vuelve a mostrar datos vigentes cuando el polling se recupera", async () => {
    mocks.obtenerTurnos
      .mockResolvedValueOnce([turno])
      .mockRejectedValueOnce(new Error("Error interno de la API."))
      .mockResolvedValue([turno]);

    render(<ColaTurnosView />);
    await esperar();
    await esperar(INTERVALO_POLLING_MS);
    expect(screen.getByText(/Datos desactualizados/)).toBeTruthy();

    await esperar(INTERVALO_POLLING_MS);
    expect(screen.queryByText(/Datos desactualizados/)).toBeNull();
  });

  it("con un filtro incompleto lo informa y no muestra la lista sin filtrar", async () => {
    mocks.obtenerTurnos.mockResolvedValue([turno]);

    render(<ColaTurnosView />);
    await esperar();
    fireEvent.change(screen.getByPlaceholderText("Teléfono +549..."), { target: { value: "+5493" } });
    await esperar(INTERVALO_POLLING_MS);

    expect(screen.getByText(/Filtro incompleto/)).toBeTruthy();
    expect(screen.queryByText("AF123BC")).toBeNull();
    expect(screen.queryByText(SIN_COINCIDENCIAS)).toBeNull();
  });

  it("envía el estado que elige el operador, sin calcular el siguiente", async () => {
    mocks.obtenerTurnos.mockResolvedValue([turno]);
    mocks.cambiarEstado.mockResolvedValue({ ...turno, estado: "cancha" });
    vi.spyOn(window, "confirm").mockReturnValue(true);

    render(<ColaTurnosView />);
    await esperar();
    const selector = screen.getByRole("combobox", { name: "Cambiar estado" });
    // Ofrece los estados del contrato salvo el actual, sin filtrar por secuencia.
    const opciones = Array.from((selector as HTMLSelectElement).options).map((o) => o.value);
    expect(opciones).toEqual(["", "viaje", "cancha", "ingresado", "descargando", "completado", "cancelado"]);

    fireEvent.change(selector, { target: { value: "cancha" } });
    await esperar();

    expect(mocks.cambiarEstado).toHaveBeenCalledWith("t1", "cancha");
  });
});

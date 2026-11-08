import { act, cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Turno } from "../../domain/entities/Turno";

const mocks = vi.hoisted(() => ({ obtenerTurnos: vi.fn() }));

vi.mock("../../composition/container", () => ({
  container: {
    apiUrl: "http://localhost:5000",
    turnos: {
      obtenerTurnosDelDia: { execute: mocks.obtenerTurnos },
      obtenerDetalleTurno: { execute: vi.fn() },
      crearTurnoManual: { execute: vi.fn() },
      cambiarEstadoTurno: { execute: vi.fn() },
      cancelarTurno: { execute: vi.fn() },
    },
  },
}));

import { PanelGeneralView } from "./PanelGeneralView";
import { INTERVALO_POLLING_MS } from "../hooks/useColaTurnos";

const turno = (id: string, patente: string, estado: Turno["estado"]): Turno => ({
  id,
  hora: "08:00",
  offsetMin: 0,
  patente,
  chofer: "Juan Pérez",
  finca: "Finca Norte",
  horasDesdeCorte: 3,
  estado,
});

const SIN_ACTIVOS = "No hay camiones en curso.";
const esperar = (ms = 0) => act(() => vi.advanceTimersByTimeAsync(ms));

describe("PanelGeneralView", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    mocks.obtenerTurnos.mockReset();
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
  });

  it("si falla la primera carga muestra el error y no afirma que no hay camiones", async () => {
    mocks.obtenerTurnos.mockRejectedValue(new Error("No se pudo conectar con la API."));

    render(<PanelGeneralView />);
    await esperar();

    expect(screen.getByText("No se pudo conectar con la API.")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Reintentar" })).toBeTruthy();
    expect(screen.queryByText(SIN_ACTIVOS)).toBeNull();
    expect(screen.queryByText(/activos/)).toBeNull();
  });

  it("muestra solo los turnos en curso que devuelve la API", async () => {
    mocks.obtenerTurnos.mockResolvedValue([
      turno("t1", "AF123BC", "viaje"),
      turno("t2", "AF456DE", "pendiente"),
      turno("t3", "AF789FG", "completado"),
    ]);

    render(<PanelGeneralView />);
    await esperar();

    expect(screen.getByText("AF123BC")).toBeTruthy();
    expect(screen.queryByText("AF456DE")).toBeNull();
    expect(screen.queryByText("AF789FG")).toBeNull();
    expect(screen.getByText("1 activos")).toBeTruthy();
  });

  it("si falla el polling conserva la lista y la marca como desactualizada", async () => {
    mocks.obtenerTurnos
      .mockResolvedValueOnce([turno("t1", "AF123BC", "viaje")])
      .mockRejectedValue(new Error("Error interno de la API."));

    render(<PanelGeneralView />);
    await esperar();
    await esperar(INTERVALO_POLLING_MS);

    expect(screen.getByText("AF123BC")).toBeTruthy();
    expect(screen.getByText(/Datos desactualizados/)).toBeTruthy();
  });

  it("no muestra datos que la API no expone", async () => {
    mocks.obtenerTurnos.mockResolvedValue([]);

    render(<PanelGeneralView />);
    await esperar();

    expect(screen.getByText(SIN_ACTIVOS)).toBeTruthy();
    expect(screen.queryByText(/Ingenio San Ramón|Operando|Detenida|zafra/i)).toBeNull();
    // Los indicadores conservan su lugar, sin valor.
    expect(screen.getByText("Estado de la molienda").parentElement?.textContent).toContain("—");
    expect((screen.getByRole("button", { name: "Enviar aviso a la flota" }) as HTMLButtonElement).disabled).toBe(true);
  });
});

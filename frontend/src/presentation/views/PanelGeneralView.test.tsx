import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Turno } from "../../domain/entities/Turno";

const mocks = vi.hoisted(() => ({ obtenerTurnos: vi.fn(), obtenerDia: vi.fn(), cambiarEstado: vi.fn() }));

vi.mock("../../composition/container", () => ({
  container: {
    apiUrl: "http://localhost:5000",
    turnos: {
      obtenerTurnosDelDia: { execute: mocks.obtenerDia },
      obtenerTurnosActivos: { execute: mocks.obtenerTurnos },
      obtenerDetalleTurno: { execute: vi.fn() },
      crearTurnoManual: { execute: vi.fn() },
      cambiarEstadoTurno: { execute: mocks.cambiarEstado },
      cancelarTurno: { execute: vi.fn() },
    },
  },
}));

import { PanelGeneralView } from "./PanelGeneralView";
import { INTERVALO_POLLING_MS } from "../hooks/useColaTurnos";

const turno = (id: string, patente: string, estado: Turno["estado"]): Turno => ({
  id,
  ventana: { inicio: "2026-09-28T08:00:00-03:00", fin: "2026-09-28T08:30:00-03:00" },
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
    mocks.obtenerDia.mockReset();
    mocks.cambiarEstado.mockReset();
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
    vi.restoreAllMocks();
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

  it("consulta activos de cualquier fecha y muestra sus ventanas sin usar la cola diaria", async () => {
    vi.setSystemTime(new Date("2026-09-28T12:00:00Z"));
    mocks.obtenerTurnos.mockResolvedValue([
      { ...turno("ayer", "AA111BB", "viaje"), ventana: {
        inicio: "2026-09-27T23:30:00-03:00", fin: "2026-09-28T00:00:00-03:00",
      } },
      { ...turno("manana", "CC222DD", "cancha"), ventana: {
        inicio: "2026-09-29T08:00:00-03:00", fin: "2026-09-29T08:30:00-03:00",
      } },
    ]);
    render(<PanelGeneralView />);
    await esperar();
    expect(mocks.obtenerTurnos).toHaveBeenCalledWith();
    expect(mocks.obtenerDia).not.toHaveBeenCalled();
    expect(screen.getByText("2 activos")).toBeTruthy();
    expect(screen.getByText("27/09/2026 23:30 (UTC-03:00) – 28/09/2026 00:00 (UTC-03:00)")).toBeTruthy();
    expect(screen.getByText("29/09/2026 08:00 – 08:30 (UTC-03:00)")).toBeTruthy();
  });

  it.each(["completado", "cancelado"] as const)("retira un turno %s después del refresco", async (estado) => {
    mocks.obtenerTurnos
      .mockResolvedValueOnce([turno("t1", "AF123BC", "viaje")])
      .mockResolvedValue([]);
    render(<PanelGeneralView />);
    await esperar();
    expect(screen.getByText("AF123BC")).toBeTruthy();
    // El backend deja de incluirlo en active=true cuando alcanza el estado terminal.
    mocks.cambiarEstado.mockResolvedValue(turno("t1", "AF123BC", estado));
    vi.spyOn(window, "confirm").mockReturnValue(true);
    fireEvent.change(screen.getByRole("combobox", { name: "Cambiar estado" }), { target: { value: estado } });
    await esperar();
    expect(mocks.cambiarEstado).toHaveBeenCalledWith("t1", estado);
    expect(screen.queryByText("AF123BC")).toBeNull();
    expect(screen.getByText(SIN_ACTIVOS)).toBeTruthy();
    await esperar(INTERVALO_POLLING_MS);
    expect(mocks.obtenerTurnos).toHaveBeenCalledTimes(3);
    expect(mocks.obtenerDia).not.toHaveBeenCalled();
  });
});

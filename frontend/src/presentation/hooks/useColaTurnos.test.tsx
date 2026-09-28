import { act, cleanup, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Turno } from "../../domain/entities/Turno";

const obtenerTurnos = vi.hoisted(() => vi.fn());

vi.mock("../../composition/container", () => ({
  container: {
    fuenteDatos: "http",
    apiUrl: "http://localhost:5000",
    turnos: {
      obtenerTurnosDelDia: { execute: obtenerTurnos },
      obtenerDetalleTurno: { execute: vi.fn() },
      crearTurnoManual: { execute: vi.fn() },
      cambiarEstadoTurno: { execute: vi.fn() },
      cancelarTurno: { execute: vi.fn() },
      reasignarHorarioTurno: { execute: vi.fn() },
    },
  },
}));

import { INTERVALO_POLLING_MS, useColaTurnos, validarFiltros } from "./useColaTurnos";

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
};

const esperar = (ms = 0) => act(() => vi.advanceTimersByTimeAsync(ms));

describe("validarFiltros", () => {
  it.each([
    ["", ""],
    ["AF123BC", ""],
    ["", "+5493815550101"],
  ])("acepta filtros vacíos o completos (%s, %s)", (patente, telefono) => {
    expect(validarFiltros(patente, telefono)).toEqual([]);
  });

  it.each([
    ["AF12", ""],
    ["AF123BC45678", ""],
    ["", "+5493"],
    ["", "3815550101"],
  ])("marca como incompleto (%s, %s)", (patente, telefono) => {
    expect(validarFiltros(patente, telefono)).toHaveLength(1);
  });
});

describe("useColaTurnos con filtros incompletos", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    obtenerTurnos.mockReset();
    obtenerTurnos.mockResolvedValue([turno]);
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
  });

  it("no consulta ni muestra turnos mientras el teléfono está a medio escribir", async () => {
    const { result } = renderHook(() => useColaTurnos());
    await esperar();
    expect(result.current.turnos).toHaveLength(1);
    const llamadasPrevias = obtenerTurnos.mock.calls.length;

    act(() => result.current.setTelefono("+5493"));
    await esperar(INTERVALO_POLLING_MS * 2);

    expect(result.current.filtrosIncompletos).toHaveLength(1);
    expect(result.current.turnos).toEqual([]);
    // Ni la consulta inmediata ni el polling salen sin el filtro.
    expect(obtenerTurnos).toHaveBeenCalledTimes(llamadasPrevias);
  });

  it("consulta con el filtro cuando el teléfono queda completo", async () => {
    const { result } = renderHook(() => useColaTurnos());
    await esperar();

    act(() => result.current.setTelefono("+5493815550101"));
    await esperar();

    expect(result.current.filtrosIncompletos).toEqual([]);
    expect(obtenerTurnos).toHaveBeenLastCalledWith(expect.objectContaining({ telefono: "+5493815550101" }));
  });

  it("no consulta con una patente incompleta", async () => {
    const { result } = renderHook(() => useColaTurnos());
    await esperar();
    const llamadasPrevias = obtenerTurnos.mock.calls.length;

    act(() => result.current.setPatente("AF12"));
    await esperar(INTERVALO_POLLING_MS);

    expect(result.current.filtrosIncompletos).toHaveLength(1);
    expect(result.current.turnos).toEqual([]);
    expect(obtenerTurnos).toHaveBeenCalledTimes(llamadasPrevias);
  });
});

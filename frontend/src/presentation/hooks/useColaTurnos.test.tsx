import { act, cleanup, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Turno } from "../../domain/entities/Turno";

const { obtenerTurnos, obtenerDetalle, cambiarEstado, cancelar } = vi.hoisted(() => ({
  obtenerTurnos: vi.fn(),
  obtenerDetalle: vi.fn(),
  cambiarEstado: vi.fn(),
  cancelar: vi.fn(),
}));

vi.mock("../../composition/container", () => ({
  container: {
    fuenteDatos: "http",
    apiUrl: "http://localhost:5000",
    turnos: {
      obtenerTurnosDelDia: { execute: obtenerTurnos },
      obtenerDetalleTurno: { execute: obtenerDetalle },
      crearTurnoManual: { execute: vi.fn() },
      cambiarEstadoTurno: { execute: cambiarEstado },
      cancelarTurno: { execute: cancelar },
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

/** Promesa que el test resuelve o rechaza cuando quiere, para forzar el orden. */
function diferida<T>() {
  let resolver!: (valor: T) => void;
  let rechazar!: (error: unknown) => void;
  const promesa = new Promise<T>((res, rej) => {
    resolver = res;
    rechazar = rej;
  });
  return { promesa, resolver, rechazar };
}

describe("useColaTurnos con respuestas de detalle fuera de orden", () => {
  const turnoA: Turno = { ...turno, id: "A", patente: "AAA111" };
  const turnoB: Turno = { ...turno, id: "B", patente: "BBB222" };

  beforeEach(() => {
    vi.useFakeTimers();
    obtenerTurnos.mockReset();
    obtenerTurnos.mockResolvedValue([turnoA, turnoB]);
    obtenerDetalle.mockReset();
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
  });

  it("descarta el detalle de A si llega después de haber abierto B", async () => {
    const pedidoA = diferida<Turno>();
    const pedidoB = diferida<Turno>();
    obtenerDetalle.mockReturnValueOnce(pedidoA.promesa).mockReturnValueOnce(pedidoB.promesa);

    const { result } = renderHook(() => useColaTurnos());
    await esperar();
    act(() => result.current.abrirDetalle("A"));
    act(() => result.current.abrirDetalle("B"));

    await act(async () => pedidoB.resolver(turnoB));
    await act(async () => pedidoA.resolver(turnoA));

    expect(result.current.detalleId).toBe("B");
    expect(result.current.detalle?.id).toBe("B");
  });

  it("un error tardío de A no cierra el detalle de B", async () => {
    const pedidoA = diferida<Turno>();
    const pedidoB = diferida<Turno>();
    obtenerDetalle.mockReturnValueOnce(pedidoA.promesa).mockReturnValueOnce(pedidoB.promesa);

    const { result } = renderHook(() => useColaTurnos());
    await esperar();
    act(() => result.current.abrirDetalle("A"));
    act(() => result.current.abrirDetalle("B"));

    await act(async () => pedidoB.resolver(turnoB));
    await act(async () => pedidoA.rechazar(new Error("Turno inexistente")));

    expect(result.current.detalleId).toBe("B");
    expect(result.current.detalle?.id).toBe("B");
    expect(result.current.errorAccion).toBeNull();
  });

  it("en el polling, una respuesta vieja del mismo turno no pisa una más nueva", async () => {
    const inicial = diferida<Turno>();
    const pollingViejo = diferida<Turno>();
    const pollingNuevo = diferida<Turno>();
    obtenerDetalle
      .mockReturnValueOnce(inicial.promesa)
      .mockReturnValueOnce(pollingViejo.promesa)
      .mockReturnValueOnce(pollingNuevo.promesa);

    const { result } = renderHook(() => useColaTurnos());
    await esperar();
    act(() => result.current.abrirDetalle("A"));
    await act(async () => inicial.resolver(turnoA));
    await esperar(INTERVALO_POLLING_MS);
    await esperar(INTERVALO_POLLING_MS);
    expect(obtenerDetalle).toHaveBeenCalledTimes(3);

    await act(async () => pollingNuevo.resolver({ ...turnoA, estado: "viaje" }));
    await act(async () => pollingViejo.resolver({ ...turnoA, estado: "pendiente" }));

    expect(result.current.detalle?.estado).toBe("viaje");
  });

  it("ignora una respuesta que llega después de cerrar el detalle", async () => {
    const pedidoA = diferida<Turno>();
    obtenerDetalle.mockReturnValueOnce(pedidoA.promesa);

    const { result } = renderHook(() => useColaTurnos());
    await esperar();
    act(() => result.current.abrirDetalle("A"));
    act(() => result.current.cerrarDetalle());
    await act(async () => pedidoA.resolver(turnoA));

    expect(result.current.detalleId).toBeNull();
    expect(result.current.detalle).toBeNull();
  });
});

describe("useColaTurnos: lecturas de detalle en vuelo durante una mutación", () => {
  const turnoA: Turno = { ...turno, id: "A", estado: "pendiente" };

  beforeEach(() => {
    vi.useFakeTimers();
    obtenerTurnos.mockReset();
    obtenerTurnos.mockResolvedValue([turnoA]);
    obtenerDetalle.mockReset();
    cambiarEstado.mockReset();
    cancelar.mockReset();
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
  });

  /** Abre A, deja cargado el detalle y un GET de polling pendiente. */
  async function abrirConPollingPendiente() {
    const pollingViejo = diferida<Turno>();
    obtenerDetalle.mockResolvedValueOnce(turnoA).mockReturnValueOnce(pollingViejo.promesa);
    const hook = renderHook(() => useColaTurnos());
    await esperar();
    act(() => hook.result.current.abrirDetalle("A"));
    await esperar();
    await esperar(INTERVALO_POLLING_MS);
    expect(obtenerDetalle).toHaveBeenCalledTimes(2);
    return { ...hook, pollingViejo };
  }

  it("un GET iniciado antes de la transición no revierte el estado confirmado", async () => {
    const { result, pollingViejo } = await abrirConPollingPendiente();
    cambiarEstado.mockResolvedValueOnce({ ...turnoA, estado: "viaje" });

    await act(() => result.current.cambiarEstado("A", "viaje"));
    expect(result.current.detalle?.estado).toBe("viaje");

    await act(async () => pollingViejo.resolver(turnoA));
    expect(result.current.detalle?.estado).toBe("viaje");
  });

  it("un GET iniciado antes de la cancelación no revierte el estado confirmado", async () => {
    const { result, pollingViejo } = await abrirConPollingPendiente();
    cancelar.mockResolvedValueOnce({ ...turnoA, estado: "cancelado" });

    await act(() => result.current.cancelar("A"));
    await act(async () => pollingViejo.resolver(turnoA));

    expect(result.current.detalle?.estado).toBe("cancelado");
  });

  it("aplica el turno confirmado aunque el detalle todavía no hubiera cargado", async () => {
    const inicial = diferida<Turno>();
    obtenerDetalle.mockReturnValueOnce(inicial.promesa);
    cambiarEstado.mockResolvedValueOnce({ ...turnoA, estado: "viaje" });

    const { result } = renderHook(() => useColaTurnos());
    await esperar();
    act(() => result.current.abrirDetalle("A"));
    expect(result.current.detalle).toBeNull();

    await act(() => result.current.cambiarEstado("A", "viaje"));
    expect(result.current.detalle?.estado).toBe("viaje");

    await act(async () => inicial.resolver(turnoA));
    expect(result.current.detalle?.estado).toBe("viaje");
  });

  it("la cola también refleja el estado confirmado y no el de una lectura anterior", async () => {
    const listaVieja = diferida<Turno[]>();
    const { result } = renderHook(() => useColaTurnos());
    await esperar();
    obtenerTurnos.mockReturnValueOnce(listaVieja.promesa).mockResolvedValue([{ ...turnoA, estado: "viaje" }]);
    await esperar(INTERVALO_POLLING_MS);
    cambiarEstado.mockResolvedValueOnce({ ...turnoA, estado: "viaje" });

    await act(() => result.current.cambiarEstado("A", "viaje"));
    await act(async () => listaVieja.resolver([turnoA]));
    await esperar();

    expect(result.current.turnos[0].estado).toBe("viaje");
  });
});

import { act, cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Turno } from "../../domain/entities/Turno";
import type { VentanaRecepcion } from "../../domain/entities/VentanaRecepcion";

const mocks = vi.hoisted(() => ({ obtenerTurnos: vi.fn(), obtenerDia: vi.fn(), cambiarEstado: vi.fn(), obtenerVentanas: vi.fn() }));

vi.mock("../../composition/container", () => ({
  container: {
    apiUrl: "http://localhost:5000",
    ventanas: { obtenerVentanas: { execute: mocks.obtenerVentanas } },
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
import { INTERVALO_VENTANAS_MS } from "../hooks/useVentanas";

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
const ventana = (datos: Partial<VentanaRecepcion> = {}): VentanaRecepcion => ({
  id: "v1", startAt: "2026-09-28T23:15:00-03:00", endAt: "2026-09-29T00:00:00-03:00",
  capacity: 7, occupied: 2, available: 5, ...datos,
});
const cupos = (slot: HTMLElement) => within(slot).getAllByRole("definition").map((nodo) => nodo.textContent);

describe("PanelGeneralView", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    mocks.obtenerTurnos.mockReset();
    mocks.obtenerDia.mockReset();
    mocks.cambiarEstado.mockReset();
    mocks.obtenerVentanas.mockReset().mockResolvedValue([]);
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

  it("muestra todas las franjas vacías, parciales y completas con cupos y horario de la API", async () => {
    mocks.obtenerTurnos.mockResolvedValue([]);
    mocks.obtenerVentanas.mockResolvedValue([
      ventana(),
      ventana({ id: "v2", startAt: "2026-09-29T00:00:00-03:00", endAt: "2026-09-29T00:45:00-03:00", capacity: 4, occupied: 0, available: 4 }),
      ventana({ id: "v3", startAt: "2026-09-29T00:45:00-03:00", endAt: "2026-09-29T01:30:00-03:00", capacity: 4, occupied: 4, available: 0 }),
    ]);
    render(<PanelGeneralView />);
    await esperar();
    expect(mocks.obtenerVentanas).toHaveBeenCalledWith(undefined);
    const slots = within(screen.getByRole("list", { name: "Franjas y cupos" })).getAllByRole("listitem");
    expect(slots).toHaveLength(3);
    expect(slots[0].getAttribute("aria-label")).toBe("28/09/2026 23:15 (UTC-03:00) – 29/09/2026 00:00 (UTC-03:00)");
    expect(cupos(slots[0])).toEqual(["7", "2", "5"]);
    expect(cupos(slots[1])).toEqual(["4", "0", "4"]);
    expect(cupos(slots[2])).toEqual(["4", "4", "0"]);
    expect(within(slots[2]).getByText("Sin cupos")).toBeTruthy();
    expect(screen.queryByText(/Flota propia|Flota tercerizada|Demorado/)).toBeNull();
  });

  it("separa una fecha sin ventanas de un error y no cambia la consulta de camiones en curso", async () => {
    mocks.obtenerTurnos.mockResolvedValue([turno("t1", "AF123BC", "viaje")]);
    render(<PanelGeneralView />);
    await esperar();
    fireEvent.change(screen.getByLabelText("Fecha de franjas"), { target: { value: "2026-10-01" } });
    await esperar();
    expect(mocks.obtenerVentanas).toHaveBeenLastCalledWith("2026-10-01");
    expect(screen.getByText("No hay ventanas configuradas para esta fecha.")).toBeTruthy();
    expect(screen.getByText("AF123BC")).toBeTruthy();
    expect(mocks.obtenerDia).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Hoy del ingenio" }));
    await esperar();
    expect(mocks.obtenerVentanas).toHaveBeenLastCalledWith(undefined);
  });

  it("un error inicial no se presenta como día vacío y permite reintentar", async () => {
    mocks.obtenerTurnos.mockResolvedValue([]);
    mocks.obtenerVentanas.mockRejectedValueOnce(new Error("Falta la clave del dashboard.")).mockResolvedValue([]);
    render(<PanelGeneralView />);
    await esperar();
    expect(screen.getByText("Falta la clave del dashboard.")).toBeTruthy();
    expect(screen.queryByText("No hay ventanas configuradas para esta fecha.")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Reintentar franjas" }));
    await esperar();
    expect(screen.queryByText("Falta la clave del dashboard.")).toBeNull();
    expect(screen.getByText("No hay ventanas configuradas para esta fecha.")).toBeTruthy();
  });

  it("el polling conserva las últimas franjas ante una caída, las marca y recupera el cupo liberado", async () => {
    mocks.obtenerTurnos.mockResolvedValue([]);
    mocks.obtenerVentanas.mockResolvedValueOnce([ventana({ occupied: 7, available: 0 })])
      .mockRejectedValueOnce(new Error("API desconectada."))
      .mockResolvedValue([ventana({ occupied: 6, available: 1 })]);
    render(<PanelGeneralView />);
    await esperar();
    await esperar(INTERVALO_VENTANAS_MS);
    expect(screen.getByText("Franjas desactualizadas.")).toBeTruthy();
    expect(screen.getByText("Sin cupos")).toBeTruthy();
    await esperar(INTERVALO_VENTANAS_MS);
    expect(screen.queryByText("Franjas desactualizadas.")).toBeNull();
    expect(cupos(screen.getByRole("listitem"))).toEqual(["7", "6", "1"]);
  });

  it("una respuesta de una fecha anterior no reemplaza la nueva ni conserva franjas al fallar otra fecha", async () => {
    mocks.obtenerTurnos.mockResolvedValue([]);
    let resolver!: (datos: VentanaRecepcion[]) => void;
    mocks.obtenerVentanas.mockReturnValueOnce(new Promise<VentanaRecepcion[]>((resolve) => { resolver = resolve; }))
      .mockResolvedValueOnce([ventana({ id: "nueva", capacity: 9, occupied: 1, available: 8 })])
      .mockRejectedValueOnce(new Error("Error de otra fecha."));
    render(<PanelGeneralView />);
    await esperar();
    fireEvent.change(screen.getByLabelText("Fecha de franjas"), { target: { value: "2026-09-29" } });
    await esperar();
    await act(async () => resolver([ventana()]));
    expect(cupos(screen.getByRole("listitem"))).toEqual(["9", "1", "8"]);
    fireEvent.change(screen.getByLabelText("Fecha de franjas"), { target: { value: "2026-09-30" } });
    await esperar();
    expect(screen.getByText("Error de otra fecha.")).toBeTruthy();
    expect(screen.queryByRole("listitem")).toBeNull();
    expect(screen.queryByText("Franjas desactualizadas.")).toBeNull();
  });

  it("refresca las franjas inmediatamente después de cancelar desde el panel", async () => {
    mocks.obtenerTurnos.mockResolvedValue([turno("t1", "AF123BC", "viaje")]);
    mocks.obtenerVentanas.mockResolvedValueOnce([ventana({ occupied: 7, available: 0 })])
      .mockResolvedValue([ventana({ occupied: 6, available: 1 })]);
    mocks.cambiarEstado.mockResolvedValue(turno("t1", "AF123BC", "cancelado"));
    vi.spyOn(window, "confirm").mockReturnValue(true);
    render(<PanelGeneralView />);
    await esperar();
    fireEvent.change(screen.getByRole("combobox", { name: "Cambiar estado" }), { target: { value: "cancelado" } });
    await esperar();
    expect(mocks.obtenerVentanas).toHaveBeenCalledTimes(2);
    expect(cupos(screen.getByRole("listitem"))).toEqual(["7", "6", "1"]);
  });

  it("no acumula consultas lentas y cancela el polling al desmontar", async () => {
    mocks.obtenerTurnos.mockResolvedValue([]);
    mocks.obtenerVentanas.mockReturnValue(new Promise(() => {}));
    const vista = render(<PanelGeneralView />);
    await esperar(INTERVALO_VENTANAS_MS * 3);
    expect(mocks.obtenerVentanas).toHaveBeenCalledTimes(1);
    vista.unmount();
    await esperar(INTERVALO_VENTANAS_MS * 2);
    expect(mocks.obtenerVentanas).toHaveBeenCalledTimes(1);
  });
});

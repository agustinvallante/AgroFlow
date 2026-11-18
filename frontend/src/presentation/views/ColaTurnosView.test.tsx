import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Turno } from "../../domain/entities/Turno";

const mocks = vi.hoisted(() => ({
  obtenerTurnos: vi.fn(),
  crear: vi.fn(),
  cambiarEstado: vi.fn(),
}));

vi.mock("../../composition/container", () => ({
  container: {
    apiUrl: "http://localhost:5000",
    turnos: {
      obtenerTurnosDelDia: { execute: mocks.obtenerTurnos },
      obtenerDetalleTurno: { execute: vi.fn() },
      crearTurnoManual: { execute: mocks.crear },
      cambiarEstadoTurno: { execute: mocks.cambiarEstado },
      cancelarTurno: { execute: vi.fn() },
    },
    transportistas: {
      obtenerTransportistas: {
        execute: async () => [
          { id: "d1", nombre: "Juan Pérez", dni: "1", telefono: "+5493815550101", patente: "AF123BC", patentes: ["AF123BC"], activo: true, puedeSolicitarTurnos: true },
          { id: "d2", nombre: "Sin camión", dni: "2", telefono: "+5493815550199", patente: null, patentes: [], activo: true, puedeSolicitarTurnos: false },
        ],
      },
    },
    fincas: { obtenerFincas: { execute: async () => [{ id: "f1", codigo: "FINCA-NORTE", nombre: "Finca Norte" }] } },
  },
}));

import { ColaTurnosView } from "./ColaTurnosView";
import { INTERVALO_POLLING_MS } from "../hooks/useColaTurnos";

const turno: Turno = {
  id: "t1",
  ventana: { inicio: "2026-09-29T08:00:00-03:00", fin: "2026-09-29T08:30:00-03:00" },
  hora: "08:00",
  offsetMin: 0,
  patente: "AF123BC",
  chofer: "Juan Pérez",
  finca: "Finca Norte",
  horasDesdeCorte: 3,
  estado: "pendiente",
  cargaTon: 28.5,
};

const SIN_COINCIDENCIAS = "No hay turnos que coincidan con estos filtros.";
const esperar = (ms = 0) => act(() => vi.advanceTimersByTimeAsync(ms));

async function guardarTurnoValido({ cargaTon = "28.5" } = {}) {
  fireEvent.click(screen.getByRole("button", { name: "+ Nuevo turno" }));
  await esperar();
  // Sólo se ofrecen transportistas habilitados; con un camión, su patente queda elegida.
  expect(screen.queryByRole("option", { name: /Sin camión/ })).toBeNull();
  fireEvent.change(screen.getByLabelText("Transportista"), { target: { value: "+5493815550101" } });
  expect((screen.getByLabelText("Patente") as HTMLSelectElement).value).toBe("AF123BC");
  fireEvent.change(screen.getByLabelText("Finca"), { target: { value: "FINCA-NORTE" } });
  fireEvent.change(document.querySelector('input[type="datetime-local"]')!, {
    target: { value: "2026-09-28T05:00" },
  });
  fireEvent.change(screen.getByPlaceholderText("28.5"), { target: { value: cargaTon } });
  mocks.obtenerTurnos.mockResolvedValue([turno]);
  fireEvent.click(screen.getByRole("button", { name: "Guardar turno" }));
  await esperar();
}

describe("ColaTurnosView", () => {
  it("no envía un alta sin carga estimada", async () => {
    mocks.obtenerTurnos.mockResolvedValue([]);
    render(<ColaTurnosView />);
    await guardarTurnoValido({ cargaTon: "" });

    expect(screen.getByText("Ingresá la carga estimada en toneladas, mayor que cero.")).toBeTruthy();
    expect(mocks.crear).not.toHaveBeenCalled();
  });

  beforeEach(() => {
    vi.useFakeTimers();
    mocks.obtenerTurnos.mockReset();
    mocks.cambiarEstado.mockReset();
    mocks.crear.mockReset();
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

  it("confirma el alta con fecha y zona, explica los filtros y abre el detalle", async () => {
    mocks.obtenerTurnos.mockResolvedValue([]);
    mocks.crear.mockResolvedValue(turno);
    render(<ColaTurnosView />);
    await esperar();
    fireEvent.change(screen.getByPlaceholderText("Buscar por patente, chofer o finca..."), {
      target: { value: "otra finca" },
    });
    await guardarTurnoValido();

    expect(screen.getByRole("status").textContent).toContain("Turno creado para AF123BC.");
    expect(screen.getByRole("status").textContent).toContain("29/09/2026 08:00 – 08:30 (UTC-03:00)");
    expect(screen.getByRole("status").textContent).toContain("limpiamos los filtros");
    expect(screen.getByText("Detalle del turno")).toBeTruthy();
    expect(screen.getAllByText("29/09/2026 08:00 – 08:30 (UTC-03:00)")).toHaveLength(2);
    expect((document.querySelector('input[type="date"]') as HTMLInputElement).value).toBe("2026-09-29");
    expect(screen.queryByText("Cargar turno manual")).toBeNull();
    expect(mocks.obtenerTurnos).toHaveBeenLastCalledWith(expect.objectContaining({ fecha: "2026-09-29" }));
  });

  it("permite cerrar la confirmación sin quitar el turno ni cerrar su detalle", async () => {
    mocks.obtenerTurnos.mockResolvedValue([]);
    mocks.crear.mockResolvedValue(turno);
    render(<ColaTurnosView />);
    await esperar();
    await guardarTurnoValido();
    fireEvent.click(screen.getByRole("button", { name: "Cerrar confirmación" }));

    expect(screen.queryByText("Turno creado para AF123BC.")).toBeNull();
    expect(screen.getByText("Detalle del turno")).toBeTruthy();
    expect(screen.getAllByText("AF123BC")).toHaveLength(2);
  });

  it.each(["fecha", "estado", "patente", "telefono", "busqueda"])(
    "retira el aviso anterior cuando el operador cambia %s", async (filtro) => {
      mocks.obtenerTurnos.mockResolvedValue([]);
      mocks.crear.mockResolvedValue(turno);
      render(<ColaTurnosView />);
      await esperar();
      await guardarTurnoValido();

      const controles: Record<string, [HTMLElement, string]> = {
        fecha: [screen.getByTitle("Fecha de la ventana (vacío = hoy)"), "2026-09-30"],
        estado: [screen.getByRole("combobox", { name: "Filtrar por estado" }), "viaje"],
        patente: [screen.getByPlaceholderText("Patente exacta"), "ZZZ999"],
        telefono: [screen.getByPlaceholderText("Teléfono +549..."), "+5493815550199"],
        busqueda: [screen.getByPlaceholderText("Buscar por patente, chofer o finca..."), "otra finca"],
      };
      const [control, value] = controles[filtro];
      fireEvent.change(control, { target: { value } });
      await esperar();

      expect(screen.queryByText("Turno creado para AF123BC.")).toBeNull();
      expect(screen.queryByRole("button", { name: "Cerrar confirmación" })).toBeNull();
      expect(screen.getByText("Detalle del turno")).toBeTruthy();
    }
  );
});

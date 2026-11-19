import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Transportista } from "../../domain/entities/Transportista";

const mocks = vi.hoisted(() => ({
  obtener: vi.fn(), editar: vi.fn(), cambiarEstado: vi.fn(), flota: vi.fn(), asociar: vi.fn(), desasociar: vi.fn(),
}));

vi.mock("../../composition/container", () => ({
  container: {
    apiUrl: "http://localhost:5000",
    transportistas: {
      obtenerTransportistas: { execute: mocks.obtener },
      crearTransportista: { execute: vi.fn() },
      editarTransportista: { execute: mocks.editar },
      cambiarEstadoActivoTransportista: { execute: mocks.cambiarEstado },
    },
    asociaciones: {
      obtenerFlotaAsociada: { execute: mocks.flota },
      asociarCamion: { execute: mocks.asociar },
      desasociarCamion: { execute: mocks.desasociar },
    },
  },
}));

import { TransportistasView } from "./TransportistasView";

const activo: Transportista = {
  id: "d1",
  nombre: "Transportista Sintético",
  dni: "20555555555",
  patente: "AF123BC",
  patentes: ["AF123BC"],
  telefono: "+5493815550155",
  activo: true,
  puedeSolicitarTurnos: true,
};

const flotaInicial = {
  porTransportista: { d1: [{ asociacionId: "a1", camionId: "c1", patente: "AF123BC" }] },
  libres: [{ id: "c2", patente: "AG888AA", tipoFlota: "TERCEROS", activo: true }],
};

describe("TransportistasView", () => {
  beforeEach(() => {
    mocks.obtener.mockReset();
    mocks.cambiarEstado.mockReset();
    mocks.asociar.mockReset();
    mocks.desasociar.mockReset();
    mocks.flota.mockReset().mockResolvedValue(flotaInicial);
  });
  afterEach(cleanup);

  it("muestra lo que devuelve la API, sin columnas fuera del contrato", async () => {
    mocks.obtener.mockResolvedValue([activo]);

    render(<TransportistasView />);

    expect(await screen.findByText("Transportista Sintético")).toBeTruthy();
    expect(screen.getByText("AF123BC")).toBeTruthy();
    expect(screen.queryByText(/Flota|Finca|Turnos este mes|San Ramón/)).toBeNull();
    expect(screen.getByRole("button", { name: "Dar de baja" })).toBeTruthy();
  });

  it("edita un transportista activo con sus datos precargados", async () => {
    mocks.obtener.mockResolvedValue([activo]);
    mocks.editar.mockReset().mockResolvedValue({ ...activo, nombre: "Nombre Corregido" });
    render(<TransportistasView />);

    fireEvent.click(await screen.findByRole("button", { name: "Editar Transportista Sintético" }));
    fireEvent.change(screen.getByDisplayValue("Transportista Sintético"), { target: { value: "Nombre Corregido" } });
    fireEvent.click(screen.getByRole("button", { name: "Guardar transportista" }));

    expect(await screen.findByText("Nombre Corregido")).toBeTruthy();
    expect(mocks.editar).toHaveBeenCalledWith("d1", { nombre: "Nombre Corregido", dni: "20555555555", telefono: "+5493815550155" });
    expect(screen.queryByText("Editar transportista")).toBeNull();
  });

  it("reactiva el registro inactivo y espera el estado persistido antes de mostrar Dar de baja", async () => {
    const inactivo = { ...activo, activo: false, puedeSolicitarTurnos: false };
    mocks.obtener.mockResolvedValueOnce([inactivo]);
    let completarReactivacion!: (valor: Transportista) => void;
    mocks.cambiarEstado.mockReturnValue(new Promise<Transportista>((resolve) => { completarReactivacion = resolve; }));

    render(<TransportistasView />);

    const boton = await screen.findByRole<HTMLButtonElement>("button", { name: "Reactivar" });
    expect(screen.queryByRole("button", { name: /Editar|Dar de baja/ })).toBeNull();
    fireEvent.click(boton);
    expect(mocks.cambiarEstado).toHaveBeenCalledWith("d1", true);
    expect(boton.disabled).toBe(true);
    expect(screen.getByText("inhabilitado")).toBeTruthy();

    await act(async () => completarReactivacion(activo));
    expect(screen.getByRole("button", { name: "Dar de baja" })).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Reactivar" })).toBeNull();
  });

  it("mantiene disponible Reactivar si la solicitud falla", async () => {
    mocks.obtener.mockResolvedValue([{ ...activo, activo: false, puedeSolicitarTurnos: false }]);
    mocks.cambiarEstado.mockRejectedValue(new Error("No se pudo reactivar."));

    render(<TransportistasView />);
    fireEvent.click(await screen.findByRole("button", { name: "Reactivar" }));

    expect(await screen.findByText("No se pudo reactivar.")).toBeTruthy();
    expect(screen.getByRole<HTMLButtonElement>("button", { name: "Reactivar" }).disabled).toBe(false);
    expect(screen.getByText("inhabilitado")).toBeTruthy();
  });

  it("si la API falla muestra el error y no una lista vacía", async () => {
    mocks.obtener.mockRejectedValue(new Error("Falta AGROFLOW_DASHBOARD_KEY"));

    render(<TransportistasView />);

    expect(await screen.findByText("Falta AGROFLOW_DASHBOARD_KEY")).toBeTruthy();
    expect(screen.queryByText(/No hay transportistas/)).toBeNull();
  });

  it("permite reintentar una falla inicial y muestra los datos cuando vuelve la API", async () => {
    mocks.obtener.mockRejectedValueOnce(new Error("API no disponible.")).mockResolvedValue([activo]);
    render(<TransportistasView />);
    await screen.findByText("API no disponible.");
    fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));
    expect(await screen.findByText(activo.nombre)).toBeTruthy();
    expect(screen.queryByText("API no disponible.")).toBeNull();
    expect(screen.queryByRole("button", { name: "Reintentar" })).toBeNull();
  });

  it("mantiene disponible el reintento si la API sigue fallando", async () => {
    mocks.obtener.mockRejectedValue(new Error("La API sigue sin responder."));
    render(<TransportistasView />);
    await screen.findByText("La API sigue sin responder.");
    fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));
    await screen.findByText("La API sigue sin responder.");
    expect(screen.getByRole("button", { name: "Reintentar" })).toBeTruthy();
    expect(screen.queryByText(/No hay transportistas/)).toBeNull();
    expect(mocks.obtener).toHaveBeenCalledTimes(2);
  });

  it("conserva la lista y los filtros ante una falla posterior y se recupera al actualizar", async () => {
    mocks.obtener.mockResolvedValue([activo]);
    render(<TransportistasView />);
    await screen.findByText(activo.nombre);
    fireEvent.change(screen.getByPlaceholderText("Buscar por nombre, DNI o teléfono..."), {
      target: { value: "Sintético" },
    });
    mocks.obtener.mockRejectedValueOnce(new Error("Falló la actualización."));
    fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));
    await screen.findByText("Datos desactualizados.");
    expect(screen.getByText(activo.nombre)).toBeTruthy();
    expect(screen.queryByText(/No hay transportistas/)).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));
    await screen.findByText(activo.nombre);
    await waitFor(() => expect(screen.queryByText("Datos desactualizados.")).toBeNull());
    expect((screen.getByPlaceholderText("Buscar por nombre, DNI o teléfono...") as HTMLInputElement).value).toBe("Sintético");
  });

  it("descarta un listado atrasado que termina después de una actualización más reciente", async () => {
    mocks.obtener.mockResolvedValue([activo]);
    render(<TransportistasView />);
    await screen.findByText(activo.nombre);
    let resolverAnterior!: (valor: Transportista[]) => void;
    const anterior = new Promise<Transportista[]>((resolver) => { resolverAnterior = resolver; });
    mocks.obtener.mockReturnValueOnce(anterior)
      .mockResolvedValueOnce([{ ...activo, nombre: "Transportista actualizado" }]);
    fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));
    fireEvent.click(screen.getByRole("button", { name: "Actualizar" }));
    await screen.findByText("Transportista actualizado");
    await act(async () => resolverAnterior([activo]));
    expect(screen.getByText("Transportista actualizado")).toBeTruthy();
    expect(screen.queryByText(activo.nombre)).toBeNull();
  });

  it("asocia un camión libre y relee transportistas y flota", async () => {
    const sinCamion = { ...activo, id: "d2", nombre: "Transportista Nuevo", patente: null, puedeSolicitarTurnos: false };
    mocks.obtener.mockResolvedValue([sinCamion]);
    mocks.asociar.mockResolvedValue(undefined);
    render(<TransportistasView />);
    await screen.findByText("Transportista Nuevo");

    fireEvent.click(await screen.findByRole("button", { name: "Asociar camión" }));
    fireEvent.change(screen.getByLabelText("Camión para Transportista Nuevo"), { target: { value: "c2" } });
    mocks.flota.mockResolvedValue({ porTransportista: { d2: [{ asociacionId: "a2", camionId: "c2", patente: "AG888AA" }] }, libres: [] });
    mocks.obtener.mockResolvedValue([{ ...sinCamion, patente: "AG888AA", puedeSolicitarTurnos: true }]);
    fireEvent.click(screen.getByRole("button", { name: "Asociar" }));

    expect(await screen.findByText("AG888AA")).toBeTruthy();
    expect(mocks.asociar).toHaveBeenCalledWith("d2", "c2");
    await waitFor(() => expect(screen.queryByText(/sin camión asociado/)).toBeNull());
  });

  it("muestra el rechazo del backend si el camión ya tiene transportista", async () => {
    mocks.obtener.mockResolvedValue([activo]);
    mocks.asociar.mockRejectedValue(new Error("El camión ya tiene un transportista activo"));
    render(<TransportistasView />);

    fireEvent.click(await screen.findByRole("button", { name: "Asociar camión" }));
    fireEvent.change(screen.getByLabelText("Camión para Transportista Sintético"), { target: { value: "c2" } });
    fireEvent.click(screen.getByRole("button", { name: "Asociar" }));

    expect(await screen.findByText("El camión ya tiene un transportista activo")).toBeTruthy();
    expect(screen.getByText("AF123BC")).toBeTruthy();
  });

  it("desasocia sólo después de confirmar", async () => {
    mocks.obtener.mockResolvedValue([activo]);
    mocks.desasociar.mockResolvedValue(undefined);
    const confirmar = vi.spyOn(window, "confirm").mockReturnValueOnce(false).mockReturnValueOnce(true);
    render(<TransportistasView />);

    const boton = await screen.findByRole("button", { name: "Desasociar AF123BC" });
    fireEvent.click(boton);
    expect(mocks.desasociar).not.toHaveBeenCalled();

    mocks.flota.mockResolvedValue({ porTransportista: {}, libres: [] });
    fireEvent.click(boton);
    await waitFor(() => expect(mocks.desasociar).toHaveBeenCalledWith("a1"));
    await waitFor(() => expect(screen.queryByText("AF123BC")).toBeNull());
    confirmar.mockRestore();
  });

  it("no ofrece asociar a un transportista inhabilitado", async () => {
    mocks.obtener.mockResolvedValue([{ ...activo, activo: false, puedeSolicitarTurnos: false }]);
    render(<TransportistasView />);

    await screen.findByText("Transportista Sintético");
    expect(screen.queryByRole("button", { name: "Asociar camión" })).toBeNull();
  });

  it("si falla la flota conserva las patentes del listado y avisa", async () => {
    mocks.obtener.mockResolvedValue([activo]);
    mocks.flota.mockRejectedValue(new Error("Camiones no disponibles."));
    render(<TransportistasView />);

    expect(await screen.findByText("Camiones no disponibles.")).toBeTruthy();
    expect(screen.getByText("AF123BC")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Asociar camión" })).toBeNull();
  });
});

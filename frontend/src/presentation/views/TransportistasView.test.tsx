import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Transportista } from "../../domain/entities/Transportista";

const mocks = vi.hoisted(() => ({ obtener: vi.fn() }));

vi.mock("../../composition/container", () => ({
  container: {
    apiUrl: "http://localhost:5000",
    transportistas: {
      obtenerTransportistas: { execute: mocks.obtener },
      crearTransportista: { execute: vi.fn() },
      cambiarEstadoActivoTransportista: { execute: vi.fn() },
    },
  },
}));

import { TransportistasView } from "./TransportistasView";

const activo: Transportista = {
  id: "d1",
  nombre: "Transportista Sintético",
  dni: "20555555555",
  patente: "AF123BC",
  telefono: "+5493815550155",
  activo: true,
  puedeSolicitarTurnos: true,
};

describe("TransportistasView", () => {
  beforeEach(() => { mocks.obtener.mockReset(); });
  afterEach(cleanup);

  it("muestra lo que devuelve la API, sin columnas fuera del contrato", async () => {
    mocks.obtener.mockResolvedValue([activo]);

    render(<TransportistasView />);

    expect(await screen.findByText("Transportista Sintético")).toBeTruthy();
    expect(screen.getByText("AF123BC")).toBeTruthy();
    expect(screen.queryByText(/Flota|Finca|Turnos este mes|San Ramón/)).toBeNull();
    expect(screen.getByRole("button", { name: "Dar de baja" })).toBeTruthy();
  });

  it("no ofrece reactivar: la API solo publica la inhabilitación", async () => {
    mocks.obtener.mockResolvedValue([{ ...activo, activo: false, puedeSolicitarTurnos: false }]);

    render(<TransportistasView />);

    await screen.findByText("Transportista Sintético");
    expect(screen.queryByRole("button", { name: /Dar de baja|Reactivar/ })).toBeNull();
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
});

import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
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
});

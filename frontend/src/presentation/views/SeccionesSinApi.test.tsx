import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { AVISO_SIN_API } from "../components/common/sinDatos";
import { ConfiguracionView } from "./ConfiguracionView";
import { ReportesView } from "./ReportesView";

describe.each([
  ["Reportes", ReportesView, "Turnos totales"],
  ["Configuración", ConfiguracionView, "Datos del ingenio"],
])("%s sin endpoint en la API", (titulo, Vista, parte) => {
  afterEach(cleanup);

  it("conserva su diseño e informa que no hay datos", () => {
    render(<Vista />);
    expect(screen.getByRole("heading", { name: titulo })).toBeTruthy();
    expect(screen.getByText(parte)).toBeTruthy();
    expect(screen.getByText(AVISO_SIN_API)).toBeTruthy();
  });

  it("no ofrece acciones que no podrían ejecutarse", () => {
    render(<Vista />);
    for (const boton of screen.getAllByRole("button")) {
      expect((boton as HTMLButtonElement).disabled).toBe(true);
    }
  });

  it("no muestra datos de ejemplo", () => {
    const { container } = render(<Vista />);
    expect(container.textContent).not.toMatch(/San Ramón|María Costas|Conectado|simulado/i);
  });
});

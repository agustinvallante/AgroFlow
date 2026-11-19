import { act, cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ConfiguracionView } from "./ConfiguracionView";
import { ReportesView } from "./ReportesView";

const HOY = "2026-09-28";
const OTRO_DIA = "2026-09-29";
const MILL = "/api/v1/mill";
const TURNOS = "/api/v1/appointments";
const FRANJAS = "/api/v1/windows";
const ingenio = { name: "Ingenio de prueba", timeZoneId: "America/Argentina/Tucuman", localDate: HOY };
const franja = {
  id: "f-1", startAt: `${HOY}T08:00:00-03:00`, endAt: `${HOY}T08:45:00-03:00`,
  capacity: 3, occupied: 1, available: 2,
};

function api(responder: (ruta: string) => Response | Promise<Response>) {
  const fetch = vi.fn(async (url: string, init: RequestInit) => {
    const request = new URL(url);
    expect(init.method).toBe("GET");
    return responder(request.pathname + request.search);
  });
  vi.stubGlobal("fetch", fetch);
  return fetch;
}

function metrica(label: string) {
  return screen.getByText(label).closest(".metric")?.querySelector(".value")?.textContent;
}

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe("Reportes de LocalDemo", () => {
  it("consulta una sola fecha operativa, incluye terminales y actualiza estados y cupos", async () => {
    let cancelado = false;
    const peticiones: string[] = [];
    api((ruta) => {
      peticiones.push(ruta);
      if (ruta === MILL) return Response.json(ingenio);
      if (ruta === `${TURNOS}?date=${HOY}`) {
        return Response.json([{ status: "FINALIZADO" }, { status: cancelado ? "CANCELADO" : "EN_ESPERA" }]);
      }
      if (ruta === `${FRANJAS}?date=${HOY}`) {
        return Response.json([cancelado ? { ...franja, occupied: 0, available: 3 } : franja]);
      }
      throw new Error(`Solicitud inesperada: ${ruta}`);
    });
    render(<ReportesView />);
    const grafico = await screen.findByRole("region", { name: "Turnos por estado" });
    expect(within(grafico).getByText("En espera").parentElement?.textContent).toBe("En espera1");
    expect(metrica("Turnos totales")).toBe("2");
    expect(metrica("Finalizados")).toBe("1");
    expect(metrica("Cancelados")).toBe("0");
    expect(metrica("Cupos disponibles")).toBe("2");
    expect(peticiones).toEqual([MILL, `${TURNOS}?date=${HOY}`, `${FRANJAS}?date=${HOY}`]);

    cancelado = true;
    fireEvent.click(screen.getByRole("button", { name: "Actualizar reporte" }));
    await waitFor(() => expect(metrica("Cancelados")).toBe("1"));
    expect(metrica("Turnos totales")).toBe("2");
    expect(metrica("Cupos disponibles")).toBe("3");
  });

  it("mantiene la fecha elegida aunque la respuesta del día anterior llegue tarde", async () => {
    let resolverTurnosAnteriores!: (response: Response) => void;
    const anterior = new Promise<Response>((resolve) => { resolverTurnosAnteriores = resolve; });
    const fetch = api((ruta) => {
      if (ruta === MILL) return Response.json(ingenio);
      return ruta === `${TURNOS}?date=${HOY}` ? anterior : Response.json([]);
    });
    render(<ReportesView />);
    await waitFor(() => expect(fetch).toHaveBeenCalledTimes(3));
    fireEvent.change(screen.getByLabelText("Fecha del reporte"), { target: { value: OTRO_DIA } });
    expect(await screen.findByText("No hay turnos asignados a esta fecha.")).toBeTruthy();
    expect(screen.getByText("No hay franjas configuradas para esta fecha.")).toBeTruthy();
    expect(metrica("Turnos totales")).toBe("0");

    await act(async () => resolverTurnosAnteriores(Response.json([{ status: "ASIGNADO" }])));
    expect(screen.getByText(OTRO_DIA)).toBeTruthy();
    expect(metrica("Turnos totales")).toBe("0");
    expect(screen.queryByRole("region", { name: "Turnos por estado" })).toBeNull();
  });

  it("indica el error sin mostrar totales de un reporte parcial y permite reintentar", async () => {
    let hayError = true;
    api((ruta) => {
      if (ruta === MILL) return Response.json(ingenio);
      if (ruta.startsWith(FRANJAS) && hayError) {
        return Response.json({ detail: "Franjas temporalmente no disponibles." }, { status: 500 });
      }
      return Response.json([]);
    });
    render(<ReportesView />);
    expect(await screen.findByText(/Error interno de la API/)).toBeTruthy();
    expect(screen.queryByText("Turnos totales")).toBeNull();

    hayError = false;
    fireEvent.click(screen.getByRole("button", { name: "Actualizar reporte" }));
    expect(await screen.findByText("No hay turnos asignados a esta fecha.")).toBeTruthy();
  });
});

describe("Configuración de LocalDemo", () => {
  it("muestra el ingenio y la duración y capacidad efectivas, con consulta por día", async () => {
    const fetch = api((ruta) => {
      if (ruta === MILL) return Response.json(ingenio);
      if (ruta === FRANJAS) return Response.json([franja]);
      if (ruta === `${FRANJAS}?date=${OTRO_DIA}`) return Response.json([]);
      throw new Error(`Solicitud inesperada: ${ruta}`);
    });
    render(<ConfiguracionView />);
    expect(await screen.findByText(ingenio.name)).toBeTruthy();
    expect(screen.getByText(ingenio.timeZoneId)).toBeTruthy();
    const tabla = await screen.findByRole("table", { name: "Franjas efectivas de recepción" });
    expect(within(tabla).getByText(/08:00 – 08:45/)).toBeTruthy();
    expect(within(tabla).getByText("45")).toBeTruthy();
    expect(within(tabla).getByText("3")).toBeTruthy();

    fireEvent.change(screen.getByLabelText("Fecha de franjas"), { target: { value: OTRO_DIA } });
    expect(await screen.findByText("No hay franjas configuradas para esta fecha.")).toBeTruthy();
    expect(fetch.mock.calls.map(([url]) => new URL(url).pathname + new URL(url).search)).toContain(`${FRANJAS}?date=${OTRO_DIA}`);
    expect(screen.queryByRole("button", { name: /guardar/i })).toBeNull();
  });
});

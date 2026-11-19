import { act, cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({ fincas: vi.fn(), turnos: vi.fn() }));

vi.mock("../../composition/container", () => ({
  container: {
    fincas: { obtenerFincas: { execute: mocks.fincas } },
    turnos: { obtenerTurnosActivos: { execute: mocks.turnos } },
  },
}));

import { MapaView } from "./MapaView";
import { CLAVE_ALMACENAMIENTO, INTERVALO_TURNOS_MS } from "../hooks/useMapaSimulado";
import { TRAMOS_MAXIMOS } from "../../domain/services/relojMapa";

/** El catálogo que publica la API en LocalDemo. */
const FINCAS = [
  { id: "f1", codigo: "FINCA-NORTE", nombre: "Finca Norte" },
  { id: "f2", codigo: "FINCA-SUR", nombre: "Finca Sur" },
];

const escribir = (etiqueta: string, valor: string) =>
  fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valor } });
const clic = (nombre: string) => fireEvent.click(screen.getByRole("button", { name: nombre }));
const contador = (texto: RegExp) => document.querySelector(".mapa-contadores")?.textContent?.match(texto)?.[1];
const camiones = () => document.querySelectorAll(".mapa-camion circle");
const avanzar = (ms: number) =>
  act(() => {
    vi.advanceTimersByTime(ms);
  });
const MINUTO = 60_000;
const esperarLectura = (ms = INTERVALO_TURNOS_MS) =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(ms);
  });
const detalleCamion = (patente: string) => within(screen.getByRole("dialog", { name: `Detalle del camión ${patente}` }));
/** Kilómetros que le faltan según el detalle del camión. */
const restanteKm = (patente = "AF123BC") => {
  const fila = detalleCamion(patente).getByText("Restante");
  return Number(fila.nextElementSibling?.textContent?.replace(" km", "").replace(",", "."));
};
/** Minutos del día de la llegada estimada que muestra el detalle. */
const llegadaMin = (patente = "AF123BC") => {
  const [h, m] = detalleCamion(patente).getByText("Llegada").nextElementSibling!.textContent!.split(" ")[0].split(":");
  return Number(h) * 60 + Number(m);
};
const tramosGuardados = () => JSON.parse(localStorage.getItem(CLAVE_ALMACENAMIENTO) ?? "{}").tramosReloj;
const iso = (ms: number) => new Date(ms).toISOString();

/** Turno de la cola tal como lo entrega el repositorio. */
const turnoBase = {
  id: "t1",
  ventana: { inicio: "2026-11-08T16:30:00Z", fin: "2026-11-08T17:00:00Z" },
  hora: "13:30",
  offsetMin: 90,
  patente: "AF123BC",
  chofer: "Juan Pérez",
  finca: "Finca Sur",
  horasDesdeCorte: 1,
  paradas: [],
};
const pinchazo = (reportadoEn: string) => ({
  id: "i1",
  tipo: "pinchazo",
  puedeSeguir: true,
  demoraEstimadaMin: 30,
  detalle: null,
  reportadoEn,
});

/** Monta la vista y espera las fincas de la API. */
async function montar() {
  const vista = render(<MapaView />);
  await act(async () => {});
  return vista;
}

function cargarViaje(patente = "af 600 ff") {
  escribir("Patente", patente);
  clic("Agregar viaje");
}

describe("MapaView", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-11-08T15:00:00Z"));
    mocks.fincas.mockReset().mockResolvedValue(FINCAS);
    mocks.turnos.mockReset().mockResolvedValue([]);
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it("empieza sin viajes, con el ingenio de referencia y las fincas de la API", async () => {
    await montar();

    const mapa = within(screen.getByRole("img"));
    expect(mapa.getByText("Finca Norte")).toBeTruthy();
    expect(mapa.getByText("Finca Sur")).toBeTruthy();
    expect(mapa.getByText("INGENIO")).toBeTruthy();
    expect(screen.getByText("Sin viajes cargados.")).toBeTruthy();
    expect(screen.getByText("Tiempo real")).toBeTruthy();
  });

  it("si la API no publica fincas lo avisa y permite reintentar", async () => {
    mocks.fincas.mockReset().mockRejectedValueOnce(new Error("API no disponible.")).mockResolvedValue(FINCAS);
    await montar();

    expect(screen.getByRole("alert").textContent).toContain("API no disponible.");
    clic("Reintentar");
    await act(async () => {});

    expect(screen.queryByRole("alert")).toBeNull();
    expect(within(screen.getByRole("img")).getByText("Finca Norte")).toBeTruthy();
  });

  it("agrega solo el viaje de un turno asignado, que espera en su finca aunque el reloj avance", async () => {
    mocks.turnos.mockResolvedValue([{ ...turnoBase, estado: "pendiente" }]);
    await montar();

    expect(screen.getByText("AF123BC", { selector: "strong" })).toBeTruthy();
    // La fila no indica si el viaje viene de la cola o de la demo.
    expect(screen.getByText(/^Finca Sur · \d+ km$/)).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Quitar viaje AF123BC" })).toBeNull();
    expect(contador(/Programados (\d+)/)).toBe("1");
    expect(screen.getByText("Espera aviso de salida")).toBeTruthy();

    // Ni a x60 ni con +1 h sale sin que el transportista avise: pasan más de tres horas del mapa.
    clic("x60");
    await esperarLectura(3 * MINUTO);
    clic("Adelantar una hora");
    expect(contador(/Programados (\d+)/)).toBe("1");
    expect(contador(/En ruta (\d+)/)).toBe("0");
    expect(contador(/Llegaron (\d+)/)).toBe("0");
    expect(camiones().length).toBe(0);
  });

  it("un turno en camino sale a la hora de la API, espera en la puerta y llega cuando la API lo informa", async () => {
    let estado = "viaje";
    // Salió hace cinco minutos según la API.
    const salidaEn = iso(Date.now() - 5 * MINUTO);
    mocks.turnos.mockImplementation(async () => [{ ...turnoBase, estado, salidaEn }]);
    await montar();

    expect(contador(/En ruta (\d+)/)).toBe("1");
    clic("Ver camión AF123BC");
    expect(detalleCamion("AF123BC").getByText("Salida").nextElementSibling?.textContent).toBe("11:55");
    clic("Cerrar detalle");

    // A x60 llega a la hora estimada, pero sin aviso de la API espera en la puerta.
    clic("x60");
    await esperarLectura(90_000);
    expect(contador(/En la puerta (\d+)/)).toBe("1");
    expect(contador(/Llegaron (\d+)/)).toBe("0");
    expect(screen.getByText("En la puerta", { selector: "em" })).toBeTruthy();
    expect(screen.getByText("Esperando ingreso")).toBeTruthy();
    expect(within(screen.getByRole("img")).getByText("AF123BC · EN LA PUERTA")).toBeTruthy();

    // El transportista avisa que llegó: la API lo pasa a En espera.
    estado = "cancha";
    await esperarLectura();
    expect(contador(/Llegaron (\d+)/)).toBe("1");
    expect(contador(/En la puerta (\d+)/)).toBe("0");
  });

  it("si la API no informa la salida, el camión sale cuando el mapa lo ve en camino", async () => {
    mocks.turnos.mockResolvedValue([{ ...turnoBase, estado: "viaje" }]);
    await montar();

    clic("Ver camión AF123BC");
    const total = Number(detalleCamion("AF123BC").getByText("Distancia").nextElementSibling?.textContent?.replace(" km", "").replace(",", "."));
    expect(restanteKm()).toBe(total);
    await esperarLectura(3 * MINUTO);
    expect(restanteKm()).toBeLessThan(total);
  });

  it("un turno con problema en ruta figura con demora y su detalle lo explica hasta que se resuelve", async () => {
    const enViaje = { ...turnoBase, estado: "viaje", salidaEn: "2026-11-08T14:50:00Z" };
    const incidente = pinchazo("2026-11-08T14:59:00Z");
    mocks.turnos
      .mockResolvedValueOnce([{ ...enViaje, incidente }])
      .mockResolvedValue([{ ...enViaje, paradas: [{ inicio: incidente.reportadoEn, fin: "2026-11-08T15:00:00Z" }] }]);
    await montar();

    expect(contador(/Demora (\d+)/)).toBe("1");
    clic("Ver camión AF123BC");
    const detalle = detalleCamion("AF123BC");
    expect(detalle.getByText("Problema")).toBeTruthy();
    expect(detalle.getByText("Pinchazo · puede seguir · demora est. 30 min")).toBeTruthy();

    // Resuelto: en la siguiente lectura vuelve a la normalidad.
    await esperarLectura();
    expect(contador(/Demora (\d+)/)).toBe("0");
    expect(screen.queryByText("Pinchazo · puede seguir · demora est. 30 min")).toBeNull();
  });

  it("a x60 el camión con un problema en ruta se detiene donde estaba, sin volver atrás", async () => {
    const enViaje = { ...turnoBase, estado: "viaje", salidaEn: iso(Date.now()) };
    let incidente: ReturnType<typeof pinchazo> | null = null;
    mocks.turnos.mockImplementation(async () => [incidente ? { ...enViaje, incidente } : enViaje]);
    await montar();

    clic("x60");
    await esperarLectura(15_000);
    expect(contador(/En ruta (\d+)/)).toBe("1");
    clic("Ver camión AF123BC");
    const antes = restanteKm();
    expect(antes).toBeGreaterThan(0);

    // La API registra el reporte con la hora real; el mapa la convierte a su propio reloj.
    incidente = pinchazo(iso(Date.now()));
    await esperarLectura();
    expect(contador(/Demora (\d+)/)).toBe("1");
    const detenido = restanteKm();
    expect(detenido).toBe(antes);

    // Sigue quieto mientras el problema está abierto.
    await esperarLectura(30_000);
    expect(restanteKm()).toBe(detenido);
  });

  it.each([
    ["x1", 1, false],
    ["x60", 60, false],
    ["x1 con +1 h", 1, true],
    ["x60 con +1 h", 60, true],
  ])("a %s, al resolverse el problema sigue desde el mismo punto, sin saltar, y llega más tarde", async (_, x, unaHora) => {
    // Tiempo real que equivale a `min` minutos del mapa con este multiplicador, en lecturas completas.
    const real = (min: number) => Math.ceil((min * MINUTO) / x / INTERVALO_TURNOS_MS) * INTERVALO_TURNOS_MS;
    const enViaje = { ...turnoBase, estado: "viaje", salidaEn: iso(Date.now()) };
    let reportadoEn: string | null = null;
    let resuelto = false;
    let resueltoEn: string | null = null;
    // La API registra la resolución en el momento de la lectura que la trae: así un salto sería visible.
    mocks.turnos.mockImplementation(async () => {
      if (!reportadoEn) return [enViaje];
      if (!resuelto) return [{ ...enViaje, incidente: pinchazo(reportadoEn) }];
      resueltoEn ??= iso(Date.now());
      return [{ ...enViaje, paradas: [{ inicio: reportadoEn, fin: resueltoEn }] }];
    });
    await montar();
    if (x === 60) clic("x60");

    await esperarLectura(real(9));
    clic("Ver camión AF123BC");
    const llegadaSinProblema = llegadaMin();
    reportadoEn = iso(Date.now());
    await esperarLectura();
    expect(contador(/Demora (\d+)/)).toBe("1");
    const detenido = restanteKm();

    await esperarLectura(real(20));
    if (unaHora) clic("Adelantar una hora");
    expect(restanteKm()).toBe(detenido);

    resuelto = true;
    await esperarLectura();
    expect(contador(/Demora (\d+)/)).toBe("0");
    expect(contador(/En ruta (\d+)/)).toBe("1");
    expect(Math.abs(restanteKm() - detenido)).toBeLessThanOrEqual(0.1);
    expect(llegadaMin() - llegadaSinProblema).toBeGreaterThanOrEqual(unaHora ? 80 : 20);

    // Y sigue avanzando desde ahí.
    await esperarLectura(real(3));
    expect(restanteKm()).toBeLessThan(detenido);
  });

  it("muestra el viaje cargado en el mapa y la cola", async () => {
    await montar();
    cargarViaje();

    expect(screen.getByText("AF600FF", { selector: "strong" })).toBeTruthy();
    expect(contador(/En ruta (\d+)/)).toBe("1");
    expect(camiones().length).toBe(1);
  });

  it("al tocar un camión muestra su detalle y lo cierra", async () => {
    await montar();
    escribir("Finca de origen", "f2");
    cargarViaje();
    avanzar(5 * MINUTO);

    clic("Ver camión AF600FF");
    const detalle = within(screen.getByRole("dialog", { name: "Detalle del camión AF600FF" }));
    expect(detalle.getByText("En ruta")).toBeTruthy();
    expect(detalle.getByText("Finca Sur")).toBeTruthy();
    for (const dato of ["Salida", "Llegada", "Distancia", "Restante"]) expect(detalle.getByText(dato)).toBeTruthy();

    clic("Cerrar detalle");
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("oculta la etiqueta que no entra y la deja disponible al pasar el cursor", async () => {
    await montar();
    for (let i = 0; i < 6; i++) cargarViaje(`AF00${i}XX`);

    const visibles = document.querySelectorAll(".mapa-camion text").length;
    const ocultas = document.querySelectorAll(".mapa-camion title").length;

    expect(visibles).toBeGreaterThan(0);
    expect(ocultas).toBeGreaterThan(0);
    expect(visibles + ocultas).toBe(6);
  });

  it("hace un solo recorrido: llega, lo muestra y no vuelve a empezar", async () => {
    await montar();
    cargarViaje();
    clic("x60");
    const inicial = camiones()[0].getAttribute("cx");

    avanzar(1000);
    expect(camiones()[0].getAttribute("cx")).not.toBe(inicial);

    // Ninguna finca queda a más de una hora a 40 km/h; a x60, menos de un minuto real.
    avanzar(90_000);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
    expect(camiones().length).toBe(0);
    expect(within(screen.getByRole("img")).getByText(/AF600FF · LLEGÓ/)).toBeTruthy();

    avanzar(600_000);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
    expect(contador(/En ruta (\d+)/)).toBe("0");
  });

  it("a x1 avanza con el reloj real", async () => {
    await montar();
    cargarViaje();

    avanzar(MINUTO);
    expect(contador(/En ruta (\d+)/)).toBe("1");

    avanzar(90 * MINUTO);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
  });

  it("marca demora si tarda más que lo estimado y luego llega", async () => {
    await montar();
    escribir("Retraso simulado (min)", "600");
    cargarViaje();

    avanzar(2 * 60 * MINUTO);
    expect(contador(/Demora (\d+)/)).toBe("1");

    avanzar(10 * 60 * MINUTO);
    expect(contador(/Llegaron (\d+)/)).toBe("1");
    expect(contador(/Demora (\d+)/)).toBe("0");
  });

  it("conserva lo cargado al volver a la pestaña y sigue por donde iba", async () => {
    const { unmount } = await montar();
    cargarViaje();
    unmount();

    vi.setSystemTime(new Date("2026-11-08T17:00:00Z"));
    await montar();

    expect(screen.getByText("AF600FF", { selector: "strong" })).toBeTruthy();
    expect(contador(/Llegaron (\d+)/)).toBe("1");
  });

  it("rechaza datos inválidos y un camión con viaje sin terminar", async () => {
    await montar();

    escribir("Velocidad (km/h)", "0");
    cargarViaje();
    expect(screen.getByText("La velocidad debe estar entre 1 y 120 km/h.")).toBeTruthy();

    escribir("Velocidad (km/h)", "40");
    cargarViaje();
    cargarViaje("AF600FF");
    expect(screen.getByText("Ese camión ya tiene un viaje sin terminar.")).toBeTruthy();
  });

  it("rechaza una ubicación del ingenio fuera de rango", async () => {
    await montar();

    escribir("Nombre del ingenio", "Lejano");
    escribir("Latitud del ingenio", "91");
    escribir("Longitud del ingenio", "-65");
    clic("Cambiar ubicación");

    expect(screen.getByText("La latitud debe estar entre -90 y 90.")).toBeTruthy();
    expect(within(screen.getByRole("img")).queryByText("LEJANO")).toBeNull();
  });

  it.each([
    ["Velocidad (km/h)", "1e-12", "La velocidad debe estar entre 1 y 120 km/h."],
    ["Velocidad (km/h)", "Infinity", "La velocidad debe estar entre 1 y 120 km/h."],
    ["Velocidad (km/h)", "121", "La velocidad debe estar entre 1 y 120 km/h."],
    ["Sale en (min)", "-1", "La salida debe estar entre 0 y 1440 minutos."],
    ["Sale en (min)", "1e12", "La salida debe estar entre 0 y 1440 minutos."],
    ["Retraso simulado (min)", "-5", "El retraso debe estar entre 0 y 1440 minutos."],
    ["Retraso simulado (min)", "Infinity", "El retraso debe estar entre 0 y 1440 minutos."],
  ])("rechaza %s = %s sin agregar el viaje", async (campo, valor, error) => {
    await montar();

    escribir(campo, valor);
    cargarViaje();

    expect(screen.getByText(error)).toBeTruthy();
    expect(screen.getByText("Sin viajes cargados.")).toBeTruthy();
  });

  it("muestra Programado hasta la hora de salida", async () => {
    await montar();
    escribir("Sale en (min)", "30");
    cargarViaje();

    expect(contador(/Programados (\d+)/)).toBe("1");
    expect(screen.getByText("Programado", { selector: "em" })).toBeTruthy();

    avanzar(31 * MINUTO);
    expect(contador(/Programados (\d+)/)).toBe("0");
    expect(contador(/En ruta (\d+)/)).toBe("1");
  });

  it("conserva el adelanto acumulado al recargar, también el de +1 h", async () => {
    const { unmount } = await montar();
    clic("x60");
    avanzar(10_000);
    clic("Adelantar una hora");
    unmount();

    await montar();

    // 10 s a x60 adelantan 590 s, unos 10 minutos, más la hora del botón.
    expect(screen.getByText("Adelantado 70 min")).toBeTruthy();
    expect(screen.getByRole("button", { name: "x1", pressed: true })).toBeTruthy();
  });

  it("vuelve a la hora real: se borran los viajes de prueba y el turno sigue programado", async () => {
    mocks.turnos.mockResolvedValue([{ ...turnoBase, estado: "pendiente" }]);
    const { unmount } = await montar();
    // Sin adelanto no hay nada que corregir.
    expect(screen.queryByRole("button", { name: "Hora real" })).toBeNull();

    cargarViaje();
    clic("x60");
    avanzar(90_000);
    // Con el reloj adelantado llegó el viaje de prueba; el turno asignado sigue en su finca.
    expect(contador(/Llegaron (\d+)/)).toBe("1");
    expect(contador(/Programados (\d+)/)).toBe("1");

    clic("Hora real");

    expect(screen.getByText("Tiempo real")).toBeTruthy();
    expect(screen.getByRole("button", { name: "x1", pressed: true })).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Hora real" })).toBeNull();
    expect(screen.queryByText("AF600FF", { selector: "strong" })).toBeNull();
    expect(contador(/Programados (\d+)/)).toBe("1");
    expect(contador(/Llegaron (\d+)/)).toBe("0");

    // La corrección se conserva al recargar.
    unmount();
    await montar();
    expect(screen.getByText("Tiempo real")).toBeTruthy();
    expect(screen.queryByText("AF600FF", { selector: "strong" })).toBeNull();
    expect(contador(/Programados (\d+)/)).toBe("1");
  });

  it("al volver a la hora real, un camión con problema en ruta queda detenido en su posición real", async () => {
    // Con la hora real a las 15:00 el camión ya salió y todavía no llegó.
    const enViaje = {
      ...turnoBase,
      ventana: { inicio: "2026-11-08T15:20:00Z", fin: "2026-11-08T15:50:00Z" },
      estado: "viaje",
      salidaEn: "2026-11-08T14:55:00Z",
    };
    const incidente = {
      id: "i1",
      tipo: "pinchazo",
      puedeSeguir: true,
      demoraEstimadaMin: 30,
      detalle: null,
      reportadoEn: "2026-11-08T15:00:00Z",
    };
    let conIncidente = false;
    mocks.turnos.mockImplementation(async () => [conIncidente ? { ...enViaje, incidente } : enViaje]);
    await montar();

    clic("x60");
    avanzar(5_000);
    // El incidente llega con el mapa adelantado: queda registrado con esa hora.
    conIncidente = true;
    await act(async () => {
      await vi.advanceTimersByTimeAsync(INTERVALO_TURNOS_MS);
    });
    expect(contador(/Demora (\d+)/)).toBe("1");

    clic("Hora real");
    clic("Ver camión AF123BC");
    const detenido = restanteKm();
    expect(detenido).toBeGreaterThan(0);

    // A x1 sigue quieto: la hora registrada con el adelanto no lo hace avanzar.
    avanzar(2 * MINUTO);
    expect(restanteKm()).toBe(detenido);
    expect(contador(/Demora (\d+)/)).toBe("1");
  });

  it("lee la cola de turnos cada 3 segundos", async () => {
    expect(INTERVALO_TURNOS_MS).toBe(3_000);
    await montar();
    expect(mocks.turnos).toHaveBeenCalledTimes(1);

    await esperarLectura(3_000);
    expect(mocks.turnos).toHaveBeenCalledTimes(2);
    await esperarLectura(6_000);
    expect(mocks.turnos).toHaveBeenCalledTimes(4);
  });

  it("registra un tramo del reloj por cada cambio de velocidad y +1 h; Hora real lo borra", async () => {
    const salidaEn = iso(Date.now() - 5 * MINUTO);
    mocks.turnos.mockResolvedValue([{ ...turnoBase, estado: "viaje", salidaEn }]);
    await montar();
    expect(tramosGuardados() ?? []).toEqual([]);

    clic("x60");
    await esperarLectura(6_000);
    clic("Adelantar una hora");
    avanzar(1_000);
    clic("x10");
    // Lo guardado termina a x1 desde el último guardado, como si el mapa se cerrara ahí.
    await esperarLectura(6_000);
    const tramos = tramosGuardados();
    expect(tramos.map((t: { multiplicador: number }) => t.multiplicador)).toEqual([60, 60, 10, 1]);
    expect(tramos[1].adelantoInicioMs - tramos[0].adelantoInicioMs).toBe(6_000 * 59 + 60 * MINUTO);

    clic("Hora real");
    expect(tramosGuardados()).toEqual([]);
    // Sin adelanto, la posición vuelve a ser la que da la hora real de salida.
    expect(contador(/En ruta (\d+)/)).toBe("1");
  });

  it("conserva el registro del reloj al recargar, acotado a los últimos tramos", async () => {
    const { unmount } = await montar();
    for (let i = 0; i < TRAMOS_MAXIMOS + 10; i++) {
      clic(i % 2 ? "x10" : "x60");
      avanzar(100);
    }
    unmount();

    const guardados = tramosGuardados();
    expect(guardados.length).toBeLessThanOrEqual(TRAMOS_MAXIMOS);
    // Al cerrar queda a x1: el tiempo con la página cerrada no se cuenta acelerado.
    expect(guardados.at(-1).multiplicador).toBe(1);

    await montar();
    expect(tramosGuardados()).toEqual(guardados);
  });

  it("a x60 no escribe en el navegador en cada tick", async () => {
    await montar();
    clic("x60");
    const escrituras = vi.spyOn(Storage.prototype, "setItem");

    avanzar(20_000);

    // 200 ticks; el adelanto se guarda cada 5 s.
    expect(escrituras.mock.calls.length).toBeLessThanOrEqual(5);
  });

  it("sigue funcionando si el navegador no permite guardar", async () => {
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
      throw new Error("bloqueado");
    });
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
      throw new Error("bloqueado");
    });
    await montar();
    cargarViaje();

    expect(screen.getByText("AF600FF", { selector: "strong" })).toBeTruthy();
    expect(contador(/En ruta (\d+)/)).toBe("1");
  });

  it("descarta lo guardado que no es válido en lugar de romper la vista", async () => {
    const viaje = { fincaId: "f1", salidaMs: Date.now(), velocidadKmh: 40, retrasoMin: 0 };
    localStorage.setItem(
      "agroflow.mapa-simulado.v3",
      JSON.stringify({
        ingenio: { nombre: "Sin coordenadas" },
        viajes: [
          { ...viaje, id: "v1", patente: "AF100AA" },
          { ...viaje, id: "v2", patente: "AF200BB", velocidadKmh: 1e-12 },
          { ...viaje, id: "v3", patente: "AF300CC", salidaMs: 1e300 },
        ],
        siguienteId: 4,
        adelantoMs: Number.MAX_VALUE,
      }),
    );

    await montar();

    expect(screen.getByText("AF100AA", { selector: "strong" })).toBeTruthy();
    for (const patente of ["AF200BB", "AF300CC"]) expect(screen.queryByText(patente, { selector: "strong" })).toBeNull();
    expect(within(screen.getByRole("img")).getByText("INGENIO")).toBeTruthy();
    expect(screen.getByText("Tiempo real")).toBeTruthy();
  });
});

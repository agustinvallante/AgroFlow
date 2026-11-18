import { describe, expect, it } from "vitest";
import {
  cajaDeEtiqueta,
  construirRedVial,
  crearProyeccion,
  puntoEnRuta,
  ubicarEtiqueta,
  type Caja,
  type Punto,
} from "./geometriaMapa";

describe("ubicarEtiqueta", () => {
  const derecha = { x: 100, y: 100, anchor: "start" as const };
  const izquierda = { x: 90, y: 100, anchor: "end" as const };

  it("usa la primera posición libre y la deja ocupada", () => {
    const ocupadas: Caja[] = [];

    expect(ubicarEtiqueta("AF100AA", [derecha, izquierda], ocupadas, 800, 500)).toEqual(derecha);
    expect(ubicarEtiqueta("AF200BB", [derecha, izquierda], ocupadas, 800, 500)).toEqual(izquierda);
    expect(ocupadas).toHaveLength(2);
  });

  it("no ubica la etiqueta si todas las posiciones están ocupadas", () => {
    const ocupadas = [cajaDeEtiqueta("AF100AA", derecha)];

    expect(ubicarEtiqueta("AF200BB", [derecha], ocupadas, 800, 500)).toBeNull();
    expect(ocupadas).toHaveLength(1);
  });

  it("descarta las posiciones fuera del lienzo", () => {
    const alBorde = { x: 795, y: 100, anchor: "start" as const };

    expect(ubicarEtiqueta("AF100AA", [alBorde, izquierda], [], 800, 500)).toEqual(izquierda);
  });
});

describe("crearProyeccion", () => {
  it("mantiene los puntos dentro del margen y el norte arriba", () => {
    const norte = { lat: -26, lon: -65 };
    const sur = { lat: -27, lon: -64.5 };
    const proyectar = crearProyeccion([norte, sur], 800, 480, 60);

    for (const punto of [proyectar(norte), proyectar(sur)]) {
      expect(punto.x).toBeGreaterThanOrEqual(60);
      expect(punto.x).toBeLessThanOrEqual(740);
      expect(punto.y).toBeGreaterThanOrEqual(60);
      expect(punto.y).toBeLessThanOrEqual(420);
    }
    expect(proyectar(norte).y).toBeLessThan(proyectar(sur).y);
  });

  it("centra un punto único sin dividir por cero", () => {
    const punto = { lat: -26, lon: -65 };
    expect(crearProyeccion([punto], 800, 480, 60)(punto)).toEqual({ x: 400, y: 240 });
  });
});

/** Generador fijo: el caso es siempre el mismo. */
function fincasAlAzar(cantidad: number, semilla: number): Punto[] {
  let s = semilla;
  const azar = () => (s = (s * 1103515245 + 12345) % 2147483648) / 2147483648;
  return Array.from({ length: cantidad }, () => ({ x: 70 + azar() * 660, y: 70 + azar() * 360 }));
}

const cerca = (a: Punto, b: Punto) => Math.hypot(a.x - b.x, a.y - b.y) < 1e-6;

describe("construirRedVial", () => {
  const ingenio = { x: 400, y: 250 };

  it("cada recorrido parte de su finca, sigue una ruta de la red y termina en el ingenio", () => {
    for (const semilla of [1, 7, 42]) {
      const fincas = fincasAlAzar(12, semilla);
      const { rutas, caminos } = construirRedVial(ingenio, fincas, 800, 500);
      const tramosDeRuta = caminos.filter((c) => c.tipo === "ruta").flatMap((c) => c.muestras);

      rutas.forEach((ruta, i) => {
        expect(cerca(ruta.muestras[0], fincas[i])).toBe(true);
        expect(cerca(ruta.muestras[ruta.muestras.length - 1], ingenio)).toBe(true);
        // Después del acceso, cada punto del recorrido es un punto de una ruta dibujada.
        const acceso = caminos.find((c) => c.id === `acceso-${i}`)!.muestras.length;
        for (const p of ruta.muestras.slice(acceso)) expect(tramosDeRuta.some((q) => cerca(p, q))).toBe(true);
      });
    }
  });

  it("dibuja ríos y una red aunque no haya fincas", () => {
    const { rutas, caminos, rios } = construirRedVial(ingenio, [], 800, 500);

    expect(rutas).toEqual([]);
    expect(rios.length).toBeGreaterThan(0);
    expect(caminos.filter((c) => c.tipo === "ruta").length).toBeGreaterThanOrEqual(4);
  });
});

describe("puntoEnRuta", () => {
  const origen = { x: 0, y: 0 };
  const destino = { x: 300, y: 0 };
  const ruta = construirRedVial(destino, [origen], 800, 500).rutas[0];

  it("empieza en el origen y termina en el destino", () => {
    expect(puntoEnRuta(ruta, 0)).toEqual(origen);
    expect(puntoEnRuta(ruta, 1).x).toBeCloseTo(300);
    expect(puntoEnRuta(ruta, 1).y).toBeCloseTo(0);
  });

  it("limita el progreso fuera de rango", () => {
    expect(puntoEnRuta(ruta, -1)).toEqual(origen);
    expect(puntoEnRuta(ruta, 2).x).toBeCloseTo(300);
  });
});

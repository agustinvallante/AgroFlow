import { describe, expect, it } from "vitest";
import { cajaDeEtiqueta, construirRuta, crearProyeccion, puntoEnRuta, ubicarEtiqueta, type Caja } from "./geometriaMapa";

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

describe("puntoEnRuta", () => {
  const origen = { x: 0, y: 0 };
  const destino = { x: 300, y: 0 };
  const ruta = construirRuta(origen, destino, 0);

  it("empieza en el origen y termina en el destino", () => {
    expect(puntoEnRuta(ruta, 0)).toEqual(origen);
    expect(puntoEnRuta(ruta, 1).x).toBeCloseTo(300);
    expect(puntoEnRuta(ruta, 1).y).toBeCloseTo(0);
  });

  it("recorre tramos iguales con progresos iguales", () => {
    const pasos = [0, 0.25, 0.5, 0.75, 1].map((p) => puntoEnRuta(ruta, p));
    const tramos = pasos.slice(1).map((p, i) => Math.hypot(p.x - pasos[i].x, p.y - pasos[i].y));
    for (const tramo of tramos) expect(tramo).toBeCloseTo(tramos[0], -0.5);
  });

  it("alterna el lado de la curva según el índice", () => {
    const opuesta = construirRuta(origen, destino, 1);
    expect(Math.sign(puntoEnRuta(ruta, 0.5).y)).toBe(-Math.sign(puntoEnRuta(opuesta, 0.5).y));
  });

  it("limita el progreso fuera de rango", () => {
    expect(puntoEnRuta(ruta, -1)).toEqual(origen);
    expect(puntoEnRuta(ruta, 2).x).toBeCloseTo(300);
  });

  it("no falla con una ruta de largo cero", () => {
    expect(puntoEnRuta(construirRuta(origen, origen, 0), 0.5)).toEqual(origen);
  });
});

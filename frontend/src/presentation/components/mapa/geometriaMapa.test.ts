import { describe, expect, it } from "vitest";
import {
  cajaDeEtiqueta,
  construirRedRutas,
  crearProyeccion,
  puntoEnRuta,
  ubicarEtiqueta,
  type Caja,
  type Punto,
  type Tramo,
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

function cruce(a1: Punto, a2: Punto, b1: Punto, b2: Punto): Punto | null {
  const d = (a2.x - a1.x) * (b2.y - b1.y) - (a2.y - a1.y) * (b2.x - b1.x);
  if (d === 0) return null;
  const t = ((b1.x - a1.x) * (b2.y - b1.y) - (b1.y - a1.y) * (b2.x - b1.x)) / d;
  const u = ((b1.x - a1.x) * (a2.y - a1.y) - (b1.y - a1.y) * (a2.x - a1.x)) / d;
  return t > 0 && t < 1 && u > 0 && u < 1 ? { x: a1.x + t * (a2.x - a1.x), y: a1.y + t * (a2.y - a1.y) } : null;
}

/** Cruces reales entre tramos, sin contar empalmes ni el ingenio, donde se unen a propósito. */
function contarCruces(tramos: Tramo[]): number {
  const empalmes = tramos
    .filter((t) => t.id.startsWith("tronco"))
    .flatMap((t) => [t.muestras[0], t.muestras[t.muestras.length - 1]]);
  const enEmpalme = (p: Punto) => empalmes.some((e) => Math.hypot(e.x - p.x, e.y - p.y) < 25);
  let cruces = 0;
  for (let i = 0; i < tramos.length; i++) {
    for (let j = i + 1; j < tramos.length; j++) {
      const a = tramos[i].muestras;
      const b = tramos[j].muestras;
      let hay = false;
      for (let x = 1; x < a.length && !hay; x++) {
        for (let y = 1; y < b.length && !hay; y++) {
          const p = cruce(a[x - 1], a[x], b[y - 1], b[y]);
          if (p && !enEmpalme(p)) hay = true;
        }
      }
      if (hay) cruces++;
    }
  }
  return cruces;
}

describe("construirRedRutas", () => {
  const ingenio = { x: 400, y: 250 };

  it("no cruza rutas, aunque haya muchas fincas", () => {
    for (const semilla of [1, 7, 42, 99]) {
      for (const cantidad of [8, 14, 30]) {
        expect(contarCruces(construirRedRutas(ingenio, fincasAlAzar(cantidad, semilla)).tramos)).toBe(0);
      }
    }
  });

  it("usa pocos troncos compartidos y un ramal por finca", () => {
    const { tramos, rutas } = construirRedRutas(ingenio, fincasAlAzar(30, 7));
    const troncos = tramos.filter((t) => t.id.startsWith("tronco"));

    expect(rutas).toHaveLength(30);
    expect(tramos.filter((t) => t.id.startsWith("ramal"))).toHaveLength(30);
    expect(troncos.length).toBeLessThanOrEqual(8);
    expect(troncos.flatMap((t) => t.indices).sort((a, b) => a - b)).toEqual(Array.from({ length: 30 }, (_, i) => i));
  });

  it("cada ruta parte de su finca real y termina en el ingenio", () => {
    const fincas = fincasAlAzar(8, 42);
    const { rutas } = construirRedRutas(ingenio, fincas);

    rutas.forEach((ruta, i) => {
      expect(puntoEnRuta(ruta, 0).x).toBeCloseTo(fincas[i].x);
      expect(puntoEnRuta(ruta, 0).y).toBeCloseTo(fincas[i].y);
      expect(puntoEnRuta(ruta, 1).x).toBeCloseTo(ingenio.x);
      expect(puntoEnRuta(ruta, 1).y).toBeCloseTo(ingenio.y);
    });
  });

  it("no agrupa en un corredor fincas en direcciones opuestas", () => {
    const { tramos } = construirRedRutas(ingenio, [
      { x: 100, y: 250 },
      { x: 700, y: 250 },
    ]);

    expect(tramos.filter((t) => t.id.startsWith("tronco"))).toHaveLength(2);
  });

  it("no falla sin fincas", () => {
    expect(construirRedRutas(ingenio, [])).toEqual({ rutas: [], tramos: [] });
  });
});

describe("puntoEnRuta", () => {
  const origen = { x: 0, y: 0 };
  const destino = { x: 300, y: 0 };
  const ruta = construirRedRutas(destino, [origen]).rutas[0];

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

  it("limita el progreso fuera de rango", () => {
    expect(puntoEnRuta(ruta, -1)).toEqual(origen);
    expect(puntoEnRuta(ruta, 2).x).toBeCloseTo(300);
  });

  it("no falla con una ruta de largo cero", () => {
    expect(puntoEnRuta(construirRedRutas(origen, [origen]).rutas[0], 0.5)).toEqual(origen);
  });
});

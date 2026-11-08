import type { PuntoGeo } from "../../../domain/services/simulacionViajes";

export interface Punto {
  x: number;
  y: number;
}

export interface Ruta {
  d: string;
  /** Muestras de la curva y su longitud acumulada, para avanzar a velocidad constante. */
  muestras: Punto[];
  acumulado: number[];
}

const CANTIDAD_MUESTRAS = 64;

export interface Caja {
  x: number;
  y: number;
  ancho: number;
  alto: number;
}

export interface PosicionEtiqueta extends Punto {
  anchor: "start" | "middle" | "end";
}

const ANCHO_CARACTER = 7.1;
const ALTO_ETIQUETA = 14;

/** Caja aproximada del texto: la fuente es monoespaciada y `y` es su línea base. */
export function cajaDeEtiqueta(texto: string, { x, y, anchor }: PosicionEtiqueta): Caja {
  const ancho = texto.length * ANCHO_CARACTER;
  const izquierda = anchor === "start" ? x : anchor === "end" ? x - ancho : x - ancho / 2;
  return { x: izquierda, y: y - 11, ancho, alto: ALTO_ETIQUETA };
}

const seSolapan = (a: Caja, b: Caja) =>
  a.x < b.x + b.ancho && b.x < a.x + a.ancho && a.y < b.y + b.alto && b.y < a.y + a.alto;

/**
 * Primera posición libre y dentro del lienzo, en orden de preferencia; la
 * registra en `ocupadas`. `null` si no entra: la etiqueta no se dibuja.
 */
export function ubicarEtiqueta(
  texto: string,
  posiciones: PosicionEtiqueta[],
  ocupadas: Caja[],
  ancho: number,
  alto: number,
): PosicionEtiqueta | null {
  for (const posicion of posiciones) {
    const caja = cajaDeEtiqueta(texto, posicion);
    const dentro = caja.x >= 0 && caja.y >= 0 && caja.x + caja.ancho <= ancho && caja.y + caja.alto <= alto;
    if (dentro && !ocupadas.some((o) => seSolapan(caja, o))) {
      ocupadas.push(caja);
      return posicion;
    }
  }
  return null;
}

/** Ajusta coordenadas geográficas al lienzo, centradas y sin deformar. */
export function crearProyeccion(puntos: PuntoGeo[], ancho: number, alto: number, margen: number) {
  const latMedia = puntos.reduce((suma, p) => suma + p.lat, 0) / Math.max(puntos.length, 1);
  const factorLon = Math.cos((latMedia * Math.PI) / 180);
  const xs = puntos.map((p) => p.lon * factorLon);
  const ys = puntos.map((p) => -p.lat);
  const minX = Math.min(...xs);
  const minY = Math.min(...ys);
  const rangoX = Math.max(...xs) - minX;
  const rangoY = Math.max(...ys) - minY;
  const escalas = [
    rangoX > 0 ? (ancho - 2 * margen) / rangoX : Infinity,
    rangoY > 0 ? (alto - 2 * margen) / rangoY : Infinity,
  ];
  const escala = Number.isFinite(Math.min(...escalas)) ? Math.min(...escalas) : 1;
  const origenX = (ancho - rangoX * escala) / 2;
  const origenY = (alto - rangoY * escala) / 2;

  return (p: PuntoGeo): Punto => ({
    x: origenX + (p.lon * factorLon - minX) * escala,
    y: origenY + (-p.lat - minY) * escala,
  });
}

function puntoBezier(p0: Punto, c1: Punto, c2: Punto, p3: Punto, t: number): Punto {
  const u = 1 - t;
  const b0 = u * u * u;
  const b1 = 3 * u * u * t;
  const b2 = 3 * u * t * t;
  const b3 = t * t * t;
  return {
    x: b0 * p0.x + b1 * c1.x + b2 * c2.x + b3 * p3.x,
    y: b0 * p0.y + b1 * c1.y + b2 * c2.y + b3 * p3.y,
  };
}

/** Curva finca → ingenio; `indice` alterna el lado para que las rutas no se encimen. */
export function construirRuta(origen: Punto, destino: Punto, indice: number): Ruta {
  const dx = destino.x - origen.x;
  const dy = destino.y - origen.y;
  const largo = Math.hypot(dx, dy);
  const lado = indice % 2 === 0 ? 1 : -1;
  const curvatura = largo * (0.14 + 0.05 * (indice % 3)) * lado;
  const normal = largo > 0 ? { x: -dy / largo, y: dx / largo } : { x: 0, y: 0 };
  const c1 = { x: origen.x + dx * 0.33 + normal.x * curvatura, y: origen.y + dy * 0.33 + normal.y * curvatura };
  const c2 = {
    x: origen.x + dx * 0.7 + normal.x * curvatura * 0.6,
    y: origen.y + dy * 0.7 + normal.y * curvatura * 0.6,
  };

  const muestras: Punto[] = [];
  const acumulado: number[] = [];
  for (let i = 0; i <= CANTIDAD_MUESTRAS; i++) {
    const punto = puntoBezier(origen, c1, c2, destino, i / CANTIDAD_MUESTRAS);
    const previo = muestras[i - 1];
    acumulado.push(previo ? acumulado[i - 1] + Math.hypot(punto.x - previo.x, punto.y - previo.y) : 0);
    muestras.push(punto);
  }

  const n = (v: number) => v.toFixed(1);
  const d = `M ${n(origen.x)} ${n(origen.y)} C ${n(c1.x)} ${n(c1.y)}, ${n(c2.x)} ${n(c2.y)}, ${n(destino.x)} ${n(destino.y)}`;
  return { d, muestras, acumulado };
}

/** Punto a la fracción `progreso` de la longitud de la ruta. */
export function puntoEnRuta({ muestras, acumulado }: Ruta, progreso: number): Punto {
  const objetivo = Math.min(Math.max(progreso, 0), 1) * acumulado[CANTIDAD_MUESTRAS];
  let i = 1;
  while (i < CANTIDAD_MUESTRAS && acumulado[i] < objetivo) i++;
  const tramo = acumulado[i] - acumulado[i - 1];
  const t = tramo > 0 ? (objetivo - acumulado[i - 1]) / tramo : 0;
  return {
    x: muestras[i - 1].x + (muestras[i].x - muestras[i - 1].x) * t,
    y: muestras[i - 1].y + (muestras[i].y - muestras[i - 1].y) * t,
  };
}

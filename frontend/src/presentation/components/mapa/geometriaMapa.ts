import type { PuntoGeo } from "../../../domain/services/simulacionViajes";

export interface Punto {
  x: number;
  y: number;
}

export interface Ruta {
  /** Muestras del recorrido y su longitud acumulada, para avanzar a velocidad constante. */
  muestras: Punto[];
  acumulado: number[];
}

/** Trozo dibujable de la red. Un tronco se dibuja una vez aunque lo usen varias fincas. */
export interface Tramo {
  id: string;
  d: string;
  muestras: Punto[];
  /** Posiciones en `origenes` de las fincas que pasan por este tramo. */
  indices: number[];
}

export interface RedRutas {
  rutas: Ruta[];
  tramos: Tramo[];
}

const MUESTRAS_RAMAL = 40;
const MUESTRAS_TRONCO = 16;
const MAX_CORREDORES = 6;
/** Un corredor abarca menos de un cuarto de vuelta: mantiene sus ramales dentro de su sector. */
const APERTURA_MAXIMA = Math.PI / 2;
/** El ramal se une al tronco antes de la finca más cercana del corredor. */
const FRACCION_EMPALME = 0.6;
/** Curvaturas a probar en cada corredor, de la más suave a la vista hasta recto. */
const CURVATURAS_RAMAL = [0.35, 0.2, 0.1, 0];
const RADIO_EMPALME = 25;

export interface Caja {
  x: number;
  y: number;
  ancho: number;
  alto: number;
}

export interface PosicionEtiqueta extends Punto {
  anchor: "start" | "middle" | "end";
}

const ANCHO_CARACTER = 7.5;
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

function crearRuta(muestras: Punto[]): Ruta {
  const acumulado = [0];
  for (let i = 1; i < muestras.length; i++) {
    acumulado.push(acumulado[i - 1] + Math.hypot(muestras[i].x - muestras[i - 1].x, muestras[i].y - muestras[i - 1].y));
  }
  return { muestras, acumulado };
}

interface FincaPolar {
  indice: number;
  angulo: number;
  radio: number;
}

/** Corta un corredor demasiado abierto por su mayor hueco, hasta que todos entren en su sector. */
function limitarApertura(grupo: FincaPolar[]): FincaPolar[][] {
  if (grupo.length < 2 || grupo[grupo.length - 1].angulo - grupo[0].angulo <= APERTURA_MAXIMA) return [grupo];
  let corte = 1;
  for (let i = 1; i < grupo.length; i++) {
    if (grupo[i].angulo - grupo[i - 1].angulo > grupo[corte].angulo - grupo[corte - 1].angulo) corte = i;
  }
  return [...limitarApertura(grupo.slice(0, corte)), ...limitarApertura(grupo.slice(corte))];
}

/** Agrupa las fincas por dirección desde el ingenio, cortando en los mayores huecos angulares. */
function agruparEnCorredores(destino: Punto, origenes: Punto[]): FincaPolar[][] {
  const orden = origenes
    .map((p, indice) => ({
      indice,
      angulo: Math.atan2(p.y - destino.y, p.x - destino.x),
      radio: Math.hypot(p.x - destino.x, p.y - destino.y),
    }))
    .sort((a, b) => a.angulo - b.angulo);
  const total = orden.length;
  if (total === 0) return [];

  const huecos = orden.map((f, i) => ({
    despuesDe: i,
    hueco: i === total - 1 ? orden[0].angulo + 2 * Math.PI - f.angulo : orden[i + 1].angulo - f.angulo,
  }));
  const cantidad = Math.min(total, MAX_CORREDORES, Math.ceil(Math.sqrt(total)));
  const cortes = new Set([...huecos].sort((a, b) => b.hueco - a.hueco).slice(0, cantidad).map((h) => h.despuesDe));

  // Se recorre el círculo desde un corte, con ángulos continuos al dar la vuelta.
  const inicio = ([...cortes][0] + 1) % total;
  const grupos: FincaPolar[][] = [[]];
  for (let paso = 0; paso < total; paso++) {
    const posicion = (inicio + paso) % total;
    const vuelta = inicio + paso >= total ? 2 * Math.PI : 0;
    grupos[grupos.length - 1].push({ ...orden[posicion], angulo: orden[posicion].angulo + vuelta });
    if (cortes.has(posicion) && paso < total - 1) grupos.push([]);
  }
  return grupos.flatMap(limitarApertura);
}

const n = (v: number) => v.toFixed(1);

function construirRamal(origen: Punto, empalme: Punto, hacia: Punto, curvatura: number) {
  const largo = Math.hypot(empalme.x - origen.x, empalme.y - origen.y);
  const c1 = { x: origen.x + (empalme.x - origen.x) * 0.35, y: origen.y + (empalme.y - origen.y) * 0.35 };
  // Llega en la dirección del tronco, para que el empalme sea suave.
  const c2 = { x: empalme.x + hacia.x * largo * curvatura, y: empalme.y + hacia.y * largo * curvatura };
  const muestras: Punto[] = [];
  for (let i = 0; i <= MUESTRAS_RAMAL; i++) {
    muestras.push(puntoBezier(origen, c1, c2, empalme, i / MUESTRAS_RAMAL));
  }
  return {
    muestras,
    d: `M ${n(origen.x)} ${n(origen.y)} C ${n(c1.x)} ${n(c1.y)}, ${n(c2.x)} ${n(c2.y)}, ${n(empalme.x)} ${n(empalme.y)}`,
  };
}

/** Punto en que se cruzan dos segmentos, o `null`. */
function cruceDeSegmentos(a1: Punto, a2: Punto, b1: Punto, b2: Punto): Punto | null {
  const divisor = (a2.x - a1.x) * (b2.y - b1.y) - (a2.y - a1.y) * (b2.x - b1.x);
  if (divisor === 0) return null;
  const t = ((b1.x - a1.x) * (b2.y - b1.y) - (b1.y - a1.y) * (b2.x - b1.x)) / divisor;
  const u = ((b1.x - a1.x) * (a2.y - a1.y) - (b1.y - a1.y) * (a2.x - a1.x)) / divisor;
  return t > 0 && t < 1 && u > 0 && u < 1 ? { x: a1.x + t * (a2.x - a1.x), y: a1.y + t * (a2.y - a1.y) } : null;
}

/** Si dos ramales se cruzan lejos del empalme, donde se unen a propósito. */
function hayCruces(ramales: { muestras: Punto[] }[], empalme: Punto): boolean {
  for (let a = 0; a < ramales.length; a++) {
    for (let b = a + 1; b < ramales.length; b++) {
      const p = ramales[a].muestras;
      const q = ramales[b].muestras;
      for (let i = 1; i < p.length; i++) {
        for (let j = 1; j < q.length; j++) {
          const cruce = cruceDeSegmentos(p[i - 1], p[i], q[j - 1], q[j]);
          if (cruce && Math.hypot(cruce.x - empalme.x, cruce.y - empalme.y) >= RADIO_EMPALME) return true;
        }
      }
    }
  }
  return false;
}

/**
 * Red finca → ingenio en forma de árbol: cada finca sigue en su lugar real, un
 * ramal la une al tronco de su corredor y los troncos llegan al ingenio. Cada
 * corredor ocupa su propio sector angular, así que no cruza a los demás.
 */
export function construirRedRutas(destino: Punto, origenes: Punto[]): RedRutas {
  const rutas: Ruta[] = new Array(origenes.length);
  const tramos: Tramo[] = [];

  agruparEnCorredores(destino, origenes).forEach((grupo, g) => {
    const direccion = grupo.reduce((suma, f) => suma + f.angulo, 0) / grupo.length;
    const hacia = { x: Math.cos(direccion), y: Math.sin(direccion) };
    const largoTronco = Math.min(...grupo.map((f) => f.radio)) * FRACCION_EMPALME;
    const empalme = { x: destino.x + hacia.x * largoTronco, y: destino.y + hacia.y * largoTronco };

    const muestrasTronco: Punto[] = [];
    for (let i = 0; i <= MUESTRAS_TRONCO; i++) {
      const t = i / MUESTRAS_TRONCO;
      muestrasTronco.push({
        x: empalme.x + (destino.x - empalme.x) * t,
        y: empalme.y + (destino.y - empalme.y) * t,
      });
    }
    tramos.push({
      id: `tronco-${g}`,
      d: `M ${n(empalme.x)} ${n(empalme.y)} L ${n(destino.x)} ${n(destino.y)}`,
      muestras: muestrasTronco,
      indices: grupo.map((f) => f.indice),
    });

    // La curva más suave que no cruce dentro del corredor. Rectos no pueden
    // cruzarse: todos convergen en el mismo empalme.
    const ramales =
      CURVATURAS_RAMAL.map((curvatura) => grupo.map((f) => construirRamal(origenes[f.indice], empalme, hacia, curvatura))).find(
        (candidatos) => !hayCruces(candidatos, empalme),
      ) ?? grupo.map((f) => construirRamal(origenes[f.indice], empalme, hacia, 0));

    grupo.forEach(({ indice }, i) => {
      tramos.push({ id: `ramal-${indice}`, d: ramales[i].d, muestras: ramales[i].muestras, indices: [indice] });
      rutas[indice] = crearRuta([...ramales[i].muestras, ...muestrasTronco.slice(1)]);
    });
  });

  return { rutas, tramos };
}

/** Punto a la fracción `progreso` de la longitud de la ruta. */
export function puntoEnRuta({ muestras, acumulado }: Ruta, progreso: number): Punto {
  const ultimo = muestras.length - 1;
  const objetivo = Math.min(Math.max(progreso, 0), 1) * acumulado[ultimo];
  let i = 1;
  while (i < ultimo && acumulado[i] < objetivo) i++;
  const tramo = acumulado[i] - acumulado[i - 1];
  const t = tramo > 0 ? (objetivo - acumulado[i - 1]) / tramo : 0;
  return {
    x: muestras[i - 1].x + (muestras[i].x - muestras[i - 1].x) * t,
    y: muestras[i - 1].y + (muestras[i].y - muestras[i - 1].y) * t,
  };
}

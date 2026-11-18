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

/**
 * Camino dibujado en el fondo. Las rutas salen del ingenio hasta el borde, los
 * secundarios unen rutas vecinas y los accesos llevan cada finca a su ruta.
 */
export interface Camino {
  id: string;
  tipo: "ruta" | "secundario" | "acceso";
  d: string;
  muestras: Punto[];
}

export interface Rio {
  id: string;
  d: string;
}

export interface RedVial {
  caminos: Camino[];
  rios: Rio[];
  /** Recorrido de cada finca, en el orden de `origenes`: acceso y luego su ruta hasta el ingenio. */
  rutas: Ruta[];
}

const MAX_CORREDORES = 6;
/** Un corredor abarca menos de un cuarto de vuelta: sus fincas comparten una sola ruta. */
const APERTURA_MAXIMA = Math.PI / 2;
const MUESTRAS_ACCESO = 24;
const PASO_RUTA = 8;
/** Ondulación de las rutas, para que no parezcan trazadas con regla. */
const AMPLITUD_RUTA = 9;
const LONGITUD_ONDA_RUTA = 190;
/** El acceso no empalma pegado al ingenio. */
const MUESTRAS_LIBRES_EN_INGENIO = 3;
/** Un hueco angular mayor que éste recibe una ruta sin fincas. */
const HUECO_PARA_RUTA = (2 * Math.PI) / 5;
const MINIMO_RUTAS = 4;
/**
 * Rutas que se dibujan, numeradas desde la que apunta al norte (1) en sentido
 * horario. Las que llevan a una finca se dibujan siempre.
 */
const RUTAS_VISIBLES = new Set([1, 4, 5, 7]);

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
export function cajaDeEtiqueta(
  texto: string,
  { x, y, anchor }: PosicionEtiqueta,
): Caja {
  const ancho = texto.length * ANCHO_CARACTER;
  const izquierda =
    anchor === "start" ? x : anchor === "end" ? x - ancho : x - ancho / 2;
  return { x: izquierda, y: y - 11, ancho, alto: ALTO_ETIQUETA };
}

const seSolapan = (a: Caja, b: Caja) =>
  a.x < b.x + b.ancho &&
  b.x < a.x + a.ancho &&
  a.y < b.y + b.alto &&
  b.y < a.y + a.alto;

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
    const dentro =
      caja.x >= 0 &&
      caja.y >= 0 &&
      caja.x + caja.ancho <= ancho &&
      caja.y + caja.alto <= alto;
    if (dentro && !ocupadas.some((o) => seSolapan(caja, o))) {
      ocupadas.push(caja);
      return posicion;
    }
  }
  return null;
}

/** Ajusta coordenadas geográficas al lienzo, centradas y sin deformar. */
export function crearProyeccion(
  puntos: PuntoGeo[],
  ancho: number,
  alto: number,
  margen: number,
) {
  const latMedia =
    puntos.reduce((suma, p) => suma + p.lat, 0) / Math.max(puntos.length, 1);
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
  const escala = Number.isFinite(Math.min(...escalas))
    ? Math.min(...escalas)
    : 1;
  const origenX = (ancho - rangoX * escala) / 2;
  const origenY = (alto - rangoY * escala) / 2;

  return (p: PuntoGeo): Punto => ({
    x: origenX + (p.lon * factorLon - minX) * escala,
    y: origenY + (-p.lat - minY) * escala,
  });
}

function crearRuta(muestras: Punto[]): Ruta {
  const acumulado = [0];
  for (let i = 1; i < muestras.length; i++) {
    acumulado.push(
      acumulado[i - 1] +
        Math.hypot(
          muestras[i].x - muestras[i - 1].x,
          muestras[i].y - muestras[i - 1].y,
        ),
    );
  }
  return { muestras, acumulado };
}

const n = (v: number) => v.toFixed(1);
export const polilinea = (muestras: Punto[]) =>
  muestras
    .map((p, i) => `${i === 0 ? "M" : "L"} ${n(p.x)} ${n(p.y)}`)
    .join(" ");

interface FincaPolar {
  indice: number;
  angulo: number;
}

/** Corta un corredor demasiado abierto por su mayor hueco, hasta que todos entren en su sector. */
function limitarApertura(grupo: FincaPolar[]): FincaPolar[][] {
  if (
    grupo.length < 2 ||
    grupo[grupo.length - 1].angulo - grupo[0].angulo <= APERTURA_MAXIMA
  )
    return [grupo];
  let corte = 1;
  for (let i = 1; i < grupo.length; i++) {
    if (
      grupo[i].angulo - grupo[i - 1].angulo >
      grupo[corte].angulo - grupo[corte - 1].angulo
    )
      corte = i;
  }
  return [
    ...limitarApertura(grupo.slice(0, corte)),
    ...limitarApertura(grupo.slice(corte)),
  ];
}

/** Agrupa las fincas por dirección desde el ingenio, cortando en los mayores huecos angulares. */
function agruparEnCorredores(
  destino: Punto,
  origenes: Punto[],
): FincaPolar[][] {
  const orden = origenes
    .map((p, indice) => ({
      indice,
      angulo: Math.atan2(p.y - destino.y, p.x - destino.x),
    }))
    .sort((a, b) => a.angulo - b.angulo);
  const total = orden.length;
  if (total === 0) return [];

  const huecos = orden.map((f, i) => ({
    despuesDe: i,
    hueco:
      i === total - 1
        ? orden[0].angulo + 2 * Math.PI - f.angulo
        : orden[i + 1].angulo - f.angulo,
  }));
  const cantidad = Math.min(total, MAX_CORREDORES, Math.ceil(Math.sqrt(total)));
  const cortes = new Set(
    [...huecos]
      .sort((a, b) => b.hueco - a.hueco)
      .slice(0, cantidad)
      .map((h) => h.despuesDe),
  );

  // Se recorre el círculo desde un corte, con ángulos continuos al dar la vuelta.
  const inicio = ([...cortes][0] + 1) % total;
  const grupos: FincaPolar[][] = [[]];
  for (let paso = 0; paso < total; paso++) {
    const posicion = (inicio + paso) % total;
    const vuelta = inicio + paso >= total ? 2 * Math.PI : 0;
    grupos[grupos.length - 1].push({
      ...orden[posicion],
      angulo: orden[posicion].angulo + vuelta,
    });
    if (cortes.has(posicion) && paso < total - 1) grupos.push([]);
  }
  return grupos.flatMap(limitarApertura);
}

/** Largo desde `origen` hasta salir del lienzo en la dirección `angulo`, con un poco de margen. */
function largoHastaBorde(
  origen: Punto,
  angulo: number,
  ancho: number,
  alto: number,
): number {
  const dx = Math.cos(angulo);
  const dy = Math.sin(angulo);
  const tx =
    dx > 0 ? (ancho - origen.x) / dx : dx < 0 ? -origen.x / dx : Infinity;
  const ty =
    dy > 0 ? (alto - origen.y) / dy : dy < 0 ? -origen.y / dy : Infinity;
  return Math.max(Math.min(tx, ty), 0) + 20;
}

/** Ruta desde el ingenio hasta el borde, levemente ondulada; la ondulación crece al alejarse. */
function trazarRuta(
  destino: Punto,
  angulo: number,
  ancho: number,
  alto: number,
  fase: number,
): Punto[] {
  const largo = largoHastaBorde(destino, angulo, ancho, alto);
  const dir = { x: Math.cos(angulo), y: Math.sin(angulo) };
  const normal = { x: -dir.y, y: dir.x };
  const pasos = Math.max(Math.ceil(largo / PASO_RUTA), 2);
  const muestras: Punto[] = [];
  for (let i = 0; i <= pasos; i++) {
    const s = (i / pasos) * largo;
    const desvio =
      AMPLITUD_RUTA *
      Math.sin((s / LONGITUD_ONDA_RUTA) * 2 * Math.PI + fase) *
      Math.min(s / 80, 1);
    muestras.push({
      x: destino.x + dir.x * s + normal.x * desvio,
      y: destino.y + dir.y * s + normal.y * desvio,
    });
  }
  return muestras;
}

function masCercana(muestras: Punto[], p: Punto, desde: number): number {
  let mejor = desde;
  for (let i = desde; i < muestras.length; i++) {
    const d = Math.hypot(muestras[i].x - p.x, muestras[i].y - p.y);
    if (d < Math.hypot(muestras[mejor].x - p.x, muestras[mejor].y - p.y))
      mejor = i;
  }
  return mejor;
}

/** Acceso de tierra: sale de la finca y entra a la ruta casi perpendicular, con una curva suave. */
function trazarAcceso(origen: Punto, empalme: Punto): Punto[] {
  const medio = {
    x: (origen.x + empalme.x) / 2,
    y: (origen.y + empalme.y) / 2,
  };
  const largo = Math.hypot(empalme.x - origen.x, empalme.y - origen.y);
  const control = {
    x: medio.x + (empalme.y - origen.y) * 0.15,
    y: medio.y - (empalme.x - origen.x) * 0.15,
  };
  const muestras: Punto[] = [];
  const cantidad = largo < 4 ? 1 : MUESTRAS_ACCESO;
  for (let i = 0; i <= cantidad; i++) {
    const t = i / cantidad;
    const u = 1 - t;
    muestras.push({
      x: u * u * origen.x + 2 * u * t * control.x + t * t * empalme.x,
      y: u * u * origen.y + 2 * u * t * control.y + t * t * empalme.y,
    });
  }
  return muestras;
}

/** Ángulos de ruta: uno por corredor y otros en los huecos grandes, para que el fondo sea una red. */
function angulosDeRutas(
  corredores: number[],
): { angulo: number; corredor: number | null }[] {
  const rutas = corredores.map((angulo, corredor) => ({
    angulo,
    corredor: corredor as number | null,
  }));
  const base =
    rutas.length > 0 ? rutas : [{ angulo: -Math.PI / 2, corredor: null }];
  while (
    base.length < MINIMO_RUTAS ||
    huecoMayor(base).hueco > HUECO_PARA_RUTA
  ) {
    const { despuesDe, hueco } = huecoMayor(base);
    base.push({ angulo: despuesDe + hueco / 2, corredor: null });
  }
  return base;
}

/**
 * Número de cada ruta: 1 la más cercana al norte y luego en sentido horario.
 * En pantalla `y` crece hacia abajo, así que el ángulo crece en sentido horario
 * y el norte está en -90°.
 */
function numerarDesdeElNorte(angulos: number[]): number[] {
  const vuelta = 2 * Math.PI;
  const rumbo = (a: number) =>
    (((a + Math.PI / 2) % vuelta) + vuelta) % vuelta;
  const alNorte = (a: number) => Math.min(rumbo(a), vuelta - rumbo(a));
  const primera = angulos.reduce(
    (mejor, a, i) => (alNorte(a) < alNorte(angulos[mejor]) ? i : mejor),
    0,
  );
  const desdePrimera = (a: number) =>
    (((rumbo(a) - rumbo(angulos[primera])) % vuelta) + vuelta) % vuelta;
  const orden = angulos
    .map((a, i) => ({ i, giro: desdePrimera(a) }))
    .sort((a, b) => a.giro - b.giro);
  const numeros = new Array<number>(angulos.length);
  orden.forEach(({ i }, posicion) => (numeros[i] = posicion + 1));
  return numeros;
}

function huecoMayor(rutas: { angulo: number }[]) {
  const orden = rutas
    .map((r) => ((r.angulo % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI))
    .sort((a, b) => a - b);
  let mayor = { despuesDe: orden[0], hueco: 2 * Math.PI };
  for (let i = 0; i < orden.length; i++) {
    const siguiente =
      i === orden.length - 1 ? orden[0] + 2 * Math.PI : orden[i + 1];
    const hueco = orden.length === 1 ? 2 * Math.PI : siguiente - orden[i];
    if (i === 0 || hueco > mayor.hueco) mayor = { despuesDe: orden[i], hueco };
  }
  return mayor;
}

/** Ríos decorativos: uno que cruza el lienzo lejos del ingenio y un arroyo que desemboca en él. */
function trazarRios(destino: Punto, ancho: number, alto: number): Rio[] {
  const abajo = destino.y < alto * 0.55;
  const y0 = abajo
    ? Math.min(destino.y + alto * 0.3, alto - 30)
    : Math.max(destino.y - alto * 0.3, 30);
  const rio: Punto[] = [];
  for (let x = -20; x <= ancho + 20; x += 10) {
    rio.push({
      x,
      y: y0 + 22 * Math.sin(x / 115 + 1.3) + 9 * Math.sin(x / 43),
    });
  }

  return [{ id: "rio", d: polilinea(rio) }];
}

/**
 * Red vial simulada: rutas que parten del ingenio, secundarios entre rutas
 * vecinas, ríos y un acceso por finca. Cada finca queda en su lugar y su
 * recorrido sigue el acceso y luego su ruta. El trazado es ilustrativo.
 */
export function construirRedVial(
  destino: Punto,
  origenes: Punto[],
  ancho: number,
  alto: number,
): RedVial {
  const corredores = agruparEnCorredores(destino, origenes);
  const direcciones = corredores.map(
    (grupo) => grupo.reduce((suma, f) => suma + f.angulo, 0) / grupo.length,
  );
  const caminos: Camino[] = [];
  const rutas: Ruta[] = new Array(origenes.length);

  const angulos = angulosDeRutas(direcciones);
  const numeros = numerarDesdeElNorte(angulos.map((a) => a.angulo));
  // `r` conserva el índice original: cada ruta mantiene su ondulación al filtrar.
  const trazadas = angulos.flatMap(({ angulo, corredor }, r) => {
    if (corredor === null && !RUTAS_VISIBLES.has(numeros[r])) return [];
    const muestras = trazarRuta(destino, angulo, ancho, alto, r * 1.7);
    caminos.push({
      id: `ruta-${r}`,
      tipo: "ruta",
      d: polilinea(muestras),
      muestras,
    });
    return [{ angulo, corredor, muestras }];
  });

  corredores.forEach((grupo, c) => {
    const ruta = trazadas.find((t) => t.corredor === c)!.muestras;
    for (const { indice } of grupo) {
      const origen = origenes[indice];
      const k = masCercana(ruta, origen, MUESTRAS_LIBRES_EN_INGENIO);
      const acceso = trazarAcceso(origen, ruta[k]);
      caminos.push({
        id: `acceso-${indice}`,
        tipo: "acceso",
        d: polilinea(acceso),
        muestras: acceso,
      });
      rutas[indice] = crearRuta([...acceso, ...ruta.slice(0, k).reverse()]);
    }
  });

  return { caminos, rios: trazarRios(destino, ancho, alto), rutas };
}

/** Punto a la fracción `progreso` de la longitud de la ruta. */
export function puntoEnRuta(
  { muestras, acumulado }: Ruta,
  progreso: number,
): Punto {
  const ultimo = muestras.length - 1;
  if (ultimo === 0) return muestras[0];
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

/**
 * Curvas de nivel decorativas: un relieve inventado, siempre el mismo, trazado
 * con "marching squares". No representa la geografía real.
 */

const PASO = 6;
const CANTIDAD_NIVELES = 15;
/** Cada cuántas curvas se dibuja una maestra, más marcada. */
const CADA_MAESTRA = 5;

/** Cerros (altura positiva) y hondonadas (negativa): [x, y, radio, altura]. */
const RELIEVE: [number, number, number, number][] = [
  [90, 110, 120, 1.0],
  [250, 60, 90, -0.7],
  [430, 120, 130, 1.25],
  [340, 230, 70, 0.6],
  [640, 90, 110, -0.9],
  [730, 210, 130, 1.1],
  [160, 300, 130, -1.0],
  [40, 420, 100, 0.9],
  [300, 430, 110, 0.75],
  [480, 330, 120, -1.05],
  [620, 400, 100, 0.85],
  [780, 460, 90, -0.6],
  [560, 230, 60, 0.5],
];

function altura(x: number, y: number): number {
  let total = 0.16 * Math.sin(x * 0.011 + 1.2) * Math.cos(y * 0.014 - 0.6);
  for (const [cx, cy, radio, h] of RELIEVE) {
    total += h * Math.exp(-((x - cx) ** 2 + (y - cy) ** 2) / (2 * radio * radio));
  }
  return total;
}

export interface CurvaDeNivel {
  d: string;
  maestra: boolean;
}

export function generarCurvasDeNivel(ancho: number, alto: number): CurvaDeNivel[] {
  const columnas = Math.ceil(ancho / PASO);
  const filas = Math.ceil(alto / PASO);
  const valores: number[][] = [];
  let minimo = Infinity;
  let maximo = -Infinity;
  for (let f = 0; f <= filas; f++) {
    valores.push([]);
    for (let c = 0; c <= columnas; c++) {
      const v = altura(c * PASO, f * PASO);
      valores[f].push(v);
      minimo = Math.min(minimo, v);
      maximo = Math.max(maximo, v);
    }
  }

  const curvas: CurvaDeNivel[] = [];
  for (let n = 1; n <= CANTIDAD_NIVELES; n++) {
    const nivel = minimo + ((maximo - minimo) * n) / (CANTIDAD_NIVELES + 1);
    const trazos: string[] = [];
    const corte = (a: number, b: number) => (nivel - a) / (b - a);

    for (let f = 0; f < filas; f++) {
      for (let c = 0; c < columnas; c++) {
        const [nw, ne, se, sw] = [valores[f][c], valores[f][c + 1], valores[f + 1][c + 1], valores[f + 1][c]];
        const x = c * PASO;
        const y = f * PASO;
        const norte = () => `${(x + corte(nw, ne) * PASO).toFixed(1)} ${y}`;
        const este = () => `${x + PASO} ${(y + corte(ne, se) * PASO).toFixed(1)}`;
        const sur = () => `${(x + corte(sw, se) * PASO).toFixed(1)} ${y + PASO}`;
        const oeste = () => `${x} ${(y + corte(nw, sw) * PASO).toFixed(1)}`;
        const linea = (a: () => string, b: () => string) => trazos.push(`M${a()}L${b()}`);

        const caso = (nw > nivel ? 8 : 0) | (ne > nivel ? 4 : 0) | (se > nivel ? 2 : 0) | (sw > nivel ? 1 : 0);
        if (caso === 1 || caso === 14) linea(oeste, sur);
        else if (caso === 2 || caso === 13) linea(sur, este);
        else if (caso === 3 || caso === 12) linea(oeste, este);
        else if (caso === 4 || caso === 11) linea(norte, este);
        else if (caso === 6 || caso === 9) linea(norte, sur);
        else if (caso === 7 || caso === 8) linea(oeste, norte);
        else if (caso === 5) {
          linea(oeste, norte);
          linea(sur, este);
        } else if (caso === 10) {
          linea(oeste, sur);
          linea(norte, este);
        }
      }
    }
    if (trazos.length > 0) curvas.push({ d: trazos.join(""), maestra: n % CADA_MAESTRA === 0 });
  }
  return curvas;
}

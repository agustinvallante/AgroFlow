/**
 * Reloj del mapa: la hora real más un adelanto que acumulan x10, x60 y +1 h. Cada navegador registra los tramos
 * de su reloj para convertir un instante real de la API (salida, problema en ruta) a la hora de su mapa: así un
 * cambio de velocidad o un +1 h no mueve hacia atrás los viajes que ya estaban en curso. Son funciones puras; el
 * registro lo guarda la vista.
 */

/** Desde `realInicioMs` el adelanto vale `adelantoInicioMs` y crece `multiplicador - 1` ms por cada ms real. */
export interface TramoReloj {
  realInicioMs: number;
  adelantoInicioMs: number;
  multiplicador: number;
}

/** Tramos que se conservan. Si se descartan los más viejos, un viaje que empezó antes puede correrse un poco. */
export const TRAMOS_MAXIMOS = 50;

const MULTIPLICADORES_VALIDOS = [1, 10, 60];
/** Tope del adelanto: unos 30 años, lejos de cualquier ensayo. */
export const ADELANTO_MAXIMO_MS = 1e12;
const INSTANTE_MAXIMO_MS = 1e15;

/**
 * Adelanto del mapa en el instante real `realMs`: el del tramo que lo cubre; antes del primero, el adelanto con el
 * que empezó; sin tramos, el actual. Nunca supera al actual, porque el adelanto sólo crece hasta volver a la hora real.
 */
export function adelantoEn(tramos: readonly TramoReloj[], realMs: number, adelantoActualMs: number): number {
  if (tramos.length === 0) return adelantoActualMs;
  let tramo = tramos[0];
  if (realMs < tramo.realInicioMs) return Math.min(tramo.adelantoInicioMs, adelantoActualMs);
  for (const t of tramos) {
    if (t.realInicioMs > realMs) break;
    tramo = t;
  }
  return Math.min(tramo.adelantoInicioMs + (realMs - tramo.realInicioMs) * (tramo.multiplicador - 1), adelantoActualMs);
}

export const horaMapaDe = (tramos: readonly TramoReloj[], realMs: number, adelantoActualMs: number) =>
  realMs + adelantoEn(tramos, realMs, adelantoActualMs);

/** Agrega un tramo al final, reemplaza los que empiezan en el mismo instante o después y conserva los últimos. */
export function agregarTramo(tramos: readonly TramoReloj[], tramo: TramoReloj): TramoReloj[] {
  return [...tramos.filter((t) => t.realInicioMs < tramo.realInicioMs), tramo].slice(-TRAMOS_MAXIMOS);
}

/**
 * Salto del reloj (+1 h): un tramo nuevo con el adelanto ya sumado. Si ningún tramo cubre lo anterior, primero se
 * registra el adelanto previo, porque antes del primer tramo vale su adelanto inicial: sin él, una salida anterior
 * tomaría el adelanto del salto y el camión volvería atrás.
 */
export function saltarReloj(
  tramos: readonly TramoReloj[],
  realMs: number,
  adelantoAntesMs: number,
  saltoMs: number,
  multiplicador: number,
): TramoReloj[] {
  const cubierto = tramos.some((t) => t.realInicioMs < realMs);
  const previos = cubierto
    ? tramos
    : [{ realInicioMs: realMs - 1, adelantoInicioMs: adelantoAntesMs, multiplicador: 1 }];
  return agregarTramo(previos, { realInicioMs: realMs, adelantoInicioMs: adelantoAntesMs + saltoMs, multiplicador });
}

/** Al cerrar el mapa la velocidad vuelve a x1: el último tramo lo refleja para el tiempo que quede cerrado. */
export function cerrarTramos(tramos: readonly TramoReloj[], realMs: number, adelantoMs: number): TramoReloj[] {
  const ultimo = tramos.at(-1);
  return !ultimo || ultimo.multiplicador === 1
    ? [...tramos]
    : agregarTramo(tramos, { realInicioMs: realMs, adelantoInicioMs: adelantoMs, multiplicador: 1 });
}

const esNumeroEntre = (valor: unknown, min: number, max: number): valor is number =>
  typeof valor === "number" && Number.isFinite(valor) && valor >= min && valor <= max;

function esTramoValido(t: unknown): t is TramoReloj {
  if (!t || typeof t !== "object") return false;
  const { realInicioMs, adelantoInicioMs, multiplicador } = t as Partial<TramoReloj>;
  return (
    esNumeroEntre(realInicioMs, 0, INSTANTE_MAXIMO_MS) &&
    esNumeroEntre(adelantoInicioMs, 0, ADELANTO_MAXIMO_MS) &&
    MULTIPLICADORES_VALIDOS.includes(multiplicador as number)
  );
}

/** Lo guardado en el navegador puede venir editado a mano: se conservan los tramos válidos en orden. */
export function sanearTramos(guardado: unknown): TramoReloj[] {
  if (!Array.isArray(guardado)) return [];
  const tramos: TramoReloj[] = [];
  for (const t of guardado) {
    if (!esTramoValido(t)) continue;
    const previo = tramos.at(-1);
    if (previo && t.realInicioMs <= previo.realInicioMs) continue;
    tramos.push({ realInicioMs: t.realInicioMs, adelantoInicioMs: t.adelantoInicioMs, multiplicador: t.multiplicador });
  }
  return tramos.slice(-TRAMOS_MAXIMOS);
}

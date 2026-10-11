export function horaConOffset(offsetMin: number): string {
  const d = new Date();
  d.setMinutes(d.getMinutes() + offsetMin);
  return d.toTimeString().slice(0, 5);
}

export function horaActual(): string {
  return new Date().toTimeString().slice(0, 8);
}

export function fechaLarga(): string {
  return new Date().toLocaleDateString("es-AR", {
    weekday: "long",
    day: "numeric",
    month: "long",
  });
}

export function generarId(prefijo: string): string {
  return `${prefijo}-${Date.now()}-${Math.floor(Math.random() * 10000)}`;
}

/*
 * Fechas de la API (RFC 3339). El contrato emite cada fecha con el
 * desplazamiento de la zona del ingenio (America/Argentina/Tucuman en la
 * demo) y pide respetarlo: la hora a mostrar es la que viene escrita, no la
 * conversión a la zona del navegador.
 */
const PATRON_RFC3339 =
  /^(\d{4})-(\d{2})-(\d{2})[Tt](\d{2}):(\d{2})(?::\d{2}(?:\.\d+)?)?([Zz]|[+-]\d{2}:\d{2})$/;

export interface FechaConDesplazamiento {
  fecha: string; // "DD/MM/AAAA"
  hora: string; // "HH:MM"
  desplazamiento: string; // "-03:00", o "+00:00" si vino con Z
}

export function leerFechaRfc3339(valor: string | undefined): FechaConDesplazamiento | null {
  const m = valor ? PATRON_RFC3339.exec(valor) : null;
  if (!m) return null;
  const [, anio, mes, dia, horas, minutos, zona] = m;
  return {
    fecha: `${dia}/${mes}/${anio}`,
    hora: `${horas}:${minutos}`,
    desplazamiento: zona.toUpperCase() === "Z" ? "+00:00" : zona,
  };
}

/** "2026-09-28T08:00:00-03:00" → "08:00", en la zona informada por la API. */
export function horaDeRfc3339(valor: string | undefined): string {
  return leerFechaRfc3339(valor)?.hora ?? "—";
}

/** "2026-09-28T08:00:00-03:00" → "28/09/2026 08:00 (UTC-03:00)". */
export function fechaHoraDeRfc3339(valor: string | undefined): string {
  const f = leerFechaRfc3339(valor);
  return f ? `${f.fecha} ${f.hora} (UTC${f.desplazamiento})` : "—";
}

/**
 * Valor de un <input type="datetime-local"> ("2026-09-28T05:30") con el
 * desplazamiento explícito de la zona del navegador, que es donde el
 * operador escribió la hora: "2026-09-28T05:30:00-03:00".
 */
export function datetimeLocalARfc3339(valor: string): string {
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2})?$/.test(valor)) return "";
  const conSegundos = valor.length === 16 ? `${valor}:00` : valor;
  const minutos = -new Date(conSegundos).getTimezoneOffset();
  const signo = minutos >= 0 ? "+" : "-";
  const abs = Math.abs(minutos);
  const desplazamiento = `${signo}${String(Math.floor(abs / 60)).padStart(2, "0")}:${String(abs % 60).padStart(2, "0")}`;
  return `${conSegundos}${desplazamiento}`;
}

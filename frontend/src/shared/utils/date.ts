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

/** Zona del ingenio sembrado en la demo local. */
export const ZONA_INGENIO = "America/Argentina/Tucuman";

const FORMATO_HORA_INGENIO = new Intl.DateTimeFormat("es-AR", {
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
  timeZone: ZONA_INGENIO,
});

/**
 * Las fechas del canal de WhatsApp llegan en UTC ("…Z"); se muestran en la
 * hora del ingenio, no en la del navegador: "2026-01-01T15:00:00Z" → "12:00".
 */
export function horaEnZonaIngenio(valorUtc: string | undefined | null): string {
  if (!valorUtc) return "—";
  const fecha = new Date(valorUtc);
  return Number.isNaN(fecha.getTime()) ? "—" : FORMATO_HORA_INGENIO.format(fecha);
}

const FORMATO_DIA_INGENIO = new Intl.DateTimeFormat("es-AR", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  timeZone: ZONA_INGENIO,
});

/**
 * Hora del ingenio, con el día cuando no es hoy (en la zona del ingenio) para
 * que un mensaje de otro día no parezca de hoy: hoy → "12:00", otro día →
 * "30/10 12:00".
 */
export function fechaHoraEnZonaIngenio(
  valorUtc: string | undefined | null,
  ahora: Date = new Date(),
): string {
  const hora = horaEnZonaIngenio(valorUtc);
  if (hora === "—") return hora;
  const fecha = new Date(valorUtc as string);
  const dia = FORMATO_DIA_INGENIO.format(fecha);
  if (dia === FORMATO_DIA_INGENIO.format(ahora)) return hora;
  return `${dia.slice(0, 5)} ${hora}`;
}

const FORMATO_DIA_SEMANA_INGENIO = new Intl.DateTimeFormat("es-AR", {
  weekday: "long",
  timeZone: ZONA_INGENIO,
});

const MS_POR_DIA = 86_400_000;

/** Días enteros del calendario en la zona del ingenio, para restar fechas. */
function numeroDeDiaEnZonaIngenio(fecha: Date): number {
  const [dia, mes, anio] = FORMATO_DIA_INGENIO.format(fecha).split("/").map(Number);
  return Date.UTC(anio, mes - 1, dia) / MS_POR_DIA;
}

/**
 * Nombre del día como en WhatsApp, en la zona del ingenio: "Hoy", "Ayer", el
 * día de la semana dentro de la última semana y, antes, "30/10/2026".
 */
export function etiquetaDiaEnZonaIngenio(
  valorUtc: string | undefined | null,
  ahora: Date = new Date(),
): string {
  if (!valorUtc) return "—";
  const fecha = new Date(valorUtc);
  if (Number.isNaN(fecha.getTime())) return "—";
  const diasAtras = numeroDeDiaEnZonaIngenio(ahora) - numeroDeDiaEnZonaIngenio(fecha);
  if (diasAtras === 0) return "Hoy";
  if (diasAtras === 1) return "Ayer";
  if (diasAtras > 1 && diasAtras < 7) {
    const nombre = FORMATO_DIA_SEMANA_INGENIO.format(fecha);
    return nombre.charAt(0).toUpperCase() + nombre.slice(1);
  }
  return FORMATO_DIA_INGENIO.format(fecha);
}

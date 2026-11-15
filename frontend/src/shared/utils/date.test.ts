import { describe, expect, it } from "vitest";
import {
  datetimeLocalARfc3339,
  etiquetaDiaEnZonaIngenio,
  fechaHoraDeRfc3339,
  fechaHoraEnZonaIngenio,
  horaDeRfc3339,
  leerFechaRfc3339,
} from "./date";

describe("fechas RFC 3339 de la API", () => {
  it("toma la hora escrita con el desplazamiento del ingenio, no la del navegador", () => {
    expect(horaDeRfc3339("2026-09-28T08:00:00-03:00")).toBe("08:00");
    // Cerca de medianoche la conversión a otra zona cambiaría el día.
    expect(leerFechaRfc3339("2026-09-28T23:30:00-03:00")).toEqual({
      fecha: "28/09/2026",
      hora: "23:30",
      desplazamiento: "-03:00",
    });
  });

  it("acepta fracciones de segundo y Z", () => {
    expect(horaDeRfc3339("2026-09-28T11:05:59.123Z")).toBe("11:05");
    expect(fechaHoraDeRfc3339("2026-09-28T11:05:00Z")).toBe("28/09/2026 11:05 (UTC+00:00)");
  });

  it("muestra el desplazamiento recibido en fecha y hora", () => {
    expect(fechaHoraDeRfc3339("2026-09-28T05:30:00-03:00")).toBe("28/09/2026 05:30 (UTC-03:00)");
  });

  it("devuelve — ante valores ausentes o sin desplazamiento", () => {
    expect(horaDeRfc3339(undefined)).toBe("—");
    expect(horaDeRfc3339("2026-09-28T08:00:00")).toBe("—");
    expect(fechaHoraDeRfc3339("no es una fecha")).toBe("—");
  });
});

describe("datetimeLocalARfc3339", () => {
  it("agrega segundos y un desplazamiento explícito que el backend acepta", () => {
    const valor = datetimeLocalARfc3339("2026-09-28T05:30");
    expect(valor).toMatch(/^2026-09-28T05:30:00[+-]\d{2}:\d{2}$/);
    // Representa el mismo instante que escribió el operador en su zona.
    expect(new Date(valor).getTime()).toBe(new Date("2026-09-28T05:30:00").getTime());
  });

  it("devuelve vacío si el campo está incompleto", () => {
    expect(datetimeLocalARfc3339("")).toBe("");
    expect(datetimeLocalARfc3339("2026-09-28")).toBe("");
  });
});

describe("fechaHoraEnZonaIngenio", () => {
  const ahora = new Date("2026-11-06T21:00:00Z");

  it("muestra solo la hora cuando es de hoy en la zona del ingenio", () => {
    expect(fechaHoraEnZonaIngenio("2026-11-06T15:00:00Z", ahora)).toBe("12:00");
  });

  it("agrega el día cuando es de otro día", () => {
    expect(fechaHoraEnZonaIngenio("2026-10-30T21:44:46Z", ahora)).toBe("30/10 18:44");
  });

  it("usa el día de la zona del ingenio, no el de UTC", () => {
    // 01:00 UTC del 07/11 son las 22:00 del 06/11 en Tucumán: sigue siendo hoy.
    expect(fechaHoraEnZonaIngenio("2026-11-07T01:00:00Z", ahora)).toBe("22:00");
  });

  it("devuelve un guion si falta la fecha o es inválida", () => {
    expect(fechaHoraEnZonaIngenio(undefined, ahora)).toBe("—");
    expect(fechaHoraEnZonaIngenio("no-es-fecha", ahora)).toBe("—");
  });
});

describe("etiquetaDiaEnZonaIngenio", () => {
  // Viernes 06/11/2026, 18:00 en Tucumán.
  const ahora = new Date("2026-11-06T21:00:00Z");

  it("nombra hoy y ayer", () => {
    expect(etiquetaDiaEnZonaIngenio("2026-11-06T15:00:00Z", ahora)).toBe("Hoy");
    expect(etiquetaDiaEnZonaIngenio("2026-11-05T15:00:00Z", ahora)).toBe("Ayer");
  });

  it("usa el día de la zona del ingenio, no el de UTC", () => {
    // 01:00 UTC del 06/11 son las 22:00 del 05/11 en Tucumán.
    expect(etiquetaDiaEnZonaIngenio("2026-11-06T01:00:00Z", ahora)).toBe("Ayer");
  });

  it("muestra el día de la semana dentro de la última semana", () => {
    expect(etiquetaDiaEnZonaIngenio("2026-11-04T15:00:00Z", ahora)).toBe("Miércoles");
    expect(etiquetaDiaEnZonaIngenio("2026-10-31T15:00:00Z", ahora)).toBe("Sábado");
  });

  it("muestra la fecha completa a partir de una semana atrás", () => {
    expect(etiquetaDiaEnZonaIngenio("2026-10-30T15:00:00Z", ahora)).toBe("30/10/2026");
  });

  it("devuelve un guion si falta la fecha o es inválida", () => {
    expect(etiquetaDiaEnZonaIngenio(undefined, ahora)).toBe("—");
    expect(etiquetaDiaEnZonaIngenio("no-es-fecha", ahora)).toBe("—");
  });
});

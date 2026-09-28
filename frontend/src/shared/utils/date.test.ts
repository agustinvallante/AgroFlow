import { describe, expect, it } from "vitest";
import { datetimeLocalARfc3339, fechaHoraDeRfc3339, horaDeRfc3339, leerFechaRfc3339 } from "./date";

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

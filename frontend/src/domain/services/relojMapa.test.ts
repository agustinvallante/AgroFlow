import { describe, expect, it } from "vitest";
import {
  adelantoEn,
  agregarTramo,
  cerrarTramos,
  horaMapaDe,
  sanearTramos,
  saltarReloj,
  TRAMOS_MAXIMOS,
  type TramoReloj,
} from "./relojMapa";

const MINUTO = 60_000;
const HORA = 60 * MINUTO;
const t0 = Date.parse("2026-11-08T15:00:00Z");

describe("reloj del mapa", () => {
  it("sin tramos registrados usa el adelanto actual", () => {
    expect(adelantoEn([], t0, 0)).toBe(0);
    expect(horaMapaDe([], t0, 5 * MINUTO)).toBe(t0 + 5 * MINUTO);
  });

  it("antes del primer tramo usa el adelanto con el que empezó", () => {
    const tramos: TramoReloj[] = [{ realInicioMs: t0, adelantoInicioMs: 10 * MINUTO, multiplicador: 60 }];
    expect(adelantoEn(tramos, t0 - HORA, 2 * HORA)).toBe(10 * MINUTO);
  });

  it("dentro de un tramo acelerado el adelanto crece con su velocidad, sin pasar el actual", () => {
    const tramos: TramoReloj[] = [{ realInicioMs: t0, adelantoInicioMs: 0, multiplicador: 60 }];
    // Un minuto real a x60 adelanta 59 minutos.
    expect(adelantoEn(tramos, t0 + MINUTO, 2 * HORA)).toBe(59 * MINUTO);
    expect(adelantoEn(tramos, t0 + MINUTO, 30 * MINUTO)).toBe(30 * MINUTO);
  });

  it("+1 h separa el antes y el después: lo anterior conserva su hora del mapa", () => {
    let tramos: TramoReloj[] = [];
    tramos = agregarTramo(tramos, { realInicioMs: t0, adelantoInicioMs: 0, multiplicador: 10 });
    tramos = agregarTramo(tramos, { realInicioMs: t0 + MINUTO, adelantoInicioMs: 9 * MINUTO, multiplicador: 1 });
    tramos = agregarTramo(tramos, { realInicioMs: t0 + 2 * MINUTO, adelantoInicioMs: 9 * MINUTO + HORA, multiplicador: 1 });
    const actual = 9 * MINUTO + HORA;

    expect(horaMapaDe(tramos, t0 + 30_000, actual)).toBe(t0 + 30_000 + 4.5 * MINUTO);
    expect(horaMapaDe(tramos, t0 + 90_000, actual)).toBe(t0 + 90_000 + 9 * MINUTO);
    expect(horaMapaDe(tramos, t0 + 3 * MINUTO, actual)).toBe(t0 + 3 * MINUTO + actual);
  });

  it("+1 h sin tramos previos conserva para lo anterior el adelanto que había antes del salto", () => {
    const tramos = saltarReloj([], t0, 5 * MINUTO, HORA, 1);
    const actual = 5 * MINUTO + HORA;
    expect(horaMapaDe(tramos, t0 - 10 * MINUTO, actual)).toBe(t0 - 5 * MINUTO);
    expect(horaMapaDe(tramos, t0 + MINUTO, actual)).toBe(t0 + MINUTO + actual);
    // También si en el mismo instante se cambió la velocidad.
    const mismoInstante = saltarReloj([{ realInicioMs: t0, adelantoInicioMs: 0, multiplicador: 60 }], t0, 0, HORA, 60);
    expect(horaMapaDe(mismoInstante, t0 - MINUTO, HORA)).toBe(t0 - MINUTO);
    expect(horaMapaDe(mismoInstante, t0 + 1_000, 2 * HORA)).toBe(t0 + 1_000 + HORA + 59_000);
  });

  it("guarda como mucho los últimos tramos y reemplaza el del mismo instante", () => {
    let tramos: TramoReloj[] = [];
    for (let i = 0; i < TRAMOS_MAXIMOS + 20; i++) {
      tramos = agregarTramo(tramos, { realInicioMs: t0 + i, adelantoInicioMs: i, multiplicador: i % 2 ? 60 : 10 });
    }
    expect(tramos).toHaveLength(TRAMOS_MAXIMOS);
    expect(tramos.at(-1)?.realInicioMs).toBe(t0 + TRAMOS_MAXIMOS + 19);

    const mismo = agregarTramo(tramos, { realInicioMs: t0 + TRAMOS_MAXIMOS + 19, adelantoInicioMs: 0, multiplicador: 1 });
    expect(mismo).toHaveLength(TRAMOS_MAXIMOS);
    expect(mismo.at(-1)?.multiplicador).toBe(1);
  });

  it("al cerrar deja el último tramo a x1, para que al volver no se suponga acelerado el tiempo cerrado", () => {
    const acelerado: TramoReloj[] = [{ realInicioMs: t0, adelantoInicioMs: 0, multiplicador: 60 }];
    expect(cerrarTramos(acelerado, t0 + MINUTO, 59 * MINUTO).at(-1)).toEqual({
      realInicioMs: t0 + MINUTO,
      adelantoInicioMs: 59 * MINUTO,
      multiplicador: 1,
    });
    const normal: TramoReloj[] = [{ realInicioMs: t0, adelantoInicioMs: HORA, multiplicador: 1 }];
    expect(cerrarTramos(normal, t0 + MINUTO, HORA)).toEqual(normal);
  });

  it("descarta tramos guardados inválidos o fuera de orden", () => {
    const valido = { realInicioMs: t0, adelantoInicioMs: 0, multiplicador: 60 };
    const tramos = sanearTramos([
      valido,
      { realInicioMs: t0 - 1, adelantoInicioMs: 0, multiplicador: 10 },
      { realInicioMs: t0 + 1, adelantoInicioMs: -5, multiplicador: 10 },
      { realInicioMs: t0 + 2, adelantoInicioMs: 0, multiplicador: 7 },
      "x",
      { realInicioMs: t0 + 3, adelantoInicioMs: MINUTO, multiplicador: 1 },
    ]);
    expect(tramos).toEqual([valido, { realInicioMs: t0 + 3, adelantoInicioMs: MINUTO, multiplicador: 1 }]);
    expect(sanearTramos("nada")).toEqual([]);
  });
});

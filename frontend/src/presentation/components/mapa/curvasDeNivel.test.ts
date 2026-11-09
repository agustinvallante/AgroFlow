import { describe, expect, it } from "vitest";
import { generarCurvasDeNivel } from "./curvasDeNivel";

describe("generarCurvasDeNivel", () => {
  const curvas = generarCurvasDeNivel(800, 500);

  it("genera niveles, con algunas curvas maestras", () => {
    expect(curvas.length).toBeGreaterThan(10);
    expect(curvas.some((c) => c.maestra)).toBe(true);
    expect(curvas.some((c) => !c.maestra)).toBe(true);
  });

  it("es siempre el mismo relieve", () => {
    expect(generarCurvasDeNivel(800, 500)).toEqual(curvas);
  });

  it("no produce coordenadas inválidas ni fuera del lienzo", () => {
    for (const { d } of curvas) {
      expect(d).not.toContain("NaN");
      const numeros = d.match(/-?[\d.]+/g)!.map(Number);
      numeros.forEach((v, i) => {
        expect(v).toBeGreaterThanOrEqual(0);
        expect(v).toBeLessThanOrEqual(i % 2 === 0 ? 804 : 504);
      });
    }
  });
});

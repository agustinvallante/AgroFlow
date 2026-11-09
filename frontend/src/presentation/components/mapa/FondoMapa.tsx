import { memo } from "react";
import { generarCurvasDeNivel } from "./curvasDeNivel";

const CURVAS = generarCurvasDeNivel(800, 500);

/** El mapa se redibuja varias veces por segundo; el relieve no cambia. */
export const FondoMapa = memo(function FondoMapa() {
  return (
    <g aria-hidden="true">
      <rect width="800" height="500" className="mapa-topo-base" />
      {CURVAS.map(({ d, maestra }) => (
        <path key={d} d={d} className={`mapa-curva-nivel ${maestra ? "maestra" : ""}`} />
      ))}
    </g>
  );
});

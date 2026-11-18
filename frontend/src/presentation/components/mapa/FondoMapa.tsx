import { memo } from "react";
import type { Camino, Rio } from "./geometriaMapa";

interface FondoMapaProps {
  ancho: number;
  alto: number;
  caminos: Camino[];
  rios: Rio[];
}

/**
 * Fondo simulado: ríos y red vial. Las rutas de los camiones siguen estos
 * caminos, pero el trazado es ilustrativo, no la geografía real.
 */
export const FondoMapa = memo(function FondoMapa({ ancho, alto, caminos, rios }: FondoMapaProps) {
  const de = (tipo: Camino["tipo"]) => caminos.filter((c) => c.tipo === tipo);
  return (
    <g aria-hidden="true">
      <rect width={ancho} height={alto} className="mapa-fondo" />
      {rios.map(({ id, d }) => (
        <g key={id}>
          <path d={d} className={`mapa-rio-borde ${id}`} />
          <path d={d} className={`mapa-rio ${id}`} />
        </g>
      ))}
      {de("secundario").map(({ id, d }) => (
        <path key={id} d={d} className="mapa-camino secundario" />
      ))}
      {de("ruta").map(({ id, d }) => (
        <g key={id}>
          <path d={d} className="mapa-camino ruta-borde" />
          <path d={d} className="mapa-camino ruta" />
        </g>
      ))}
      {de("acceso").map(({ id, d }) => (
        <path key={id} d={d} className="mapa-camino acceso" />
      ))}
    </g>
  );
});

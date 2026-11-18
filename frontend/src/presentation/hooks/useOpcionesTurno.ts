import { useEffect, useState } from "react";
import { container } from "../../composition/container";
import type { Finca } from "../../domain/entities/Finca";
import type { Transportista } from "../../domain/entities/Transportista";

export interface OpcionesTurno {
  /** Sólo quienes la API habilita para pedir turnos: activos y con camión asociado. */
  transportistas: Transportista[];
  fincas: Finca[];
}

/**
 * Transportistas y fincas para el alta manual. Se piden al abrir el formulario
 * para que reflejen las altas, bajas y asociaciones más recientes.
 */
export function useOpcionesTurno(abierto: boolean) {
  const [opciones, setOpciones] = useState<OpcionesTurno | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [intento, setIntento] = useState(0);

  useEffect(() => {
    if (!abierto) return;
    let vigente = true;
    Promise.all([container.transportistas.obtenerTransportistas.execute(), container.fincas.obtenerFincas.execute()])
      .then(([transportistas, fincas]) => {
        if (!vigente) return;
        setOpciones({ transportistas: transportistas.filter((t) => t.activo && t.puedeSolicitarTurnos), fincas });
        setError(null);
      })
      .catch((e: unknown) => {
        if (vigente) setError(e instanceof Error ? e.message : "No se pudieron cargar transportistas y fincas.");
      });
    return () => {
      vigente = false;
    };
  }, [abierto, intento]);

  return { opciones, error, reintentar: () => setIntento((i) => i + 1) };
}

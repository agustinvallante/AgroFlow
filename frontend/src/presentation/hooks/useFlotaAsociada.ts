import { useCallback, useEffect, useRef, useState } from "react";
import { container } from "../../composition/container";
import type { FlotaAsociada } from "../../application/usecases/transportistas/AsociacionUseCases";

const FLOTA_VACIA: FlotaAsociada = { porTransportista: {}, libres: [] };

/**
 * Camiones asociados a cada transportista y camiones libres. Tras asociar o
 * desasociar relee la flota y avisa con `alCambiar` para que el listado de
 * transportistas refleje si cada uno puede pedir turnos.
 */
export function useFlotaAsociada(alCambiar: () => void) {
  const [flota, setFlota] = useState<FlotaAsociada>(FLOTA_VACIA);
  const [errorFlota, setErrorFlota] = useState<string | null>(null);
  const ultimaConsulta = useRef(0);
  const invalidarConsulta = useCallback(() => { ultimaConsulta.current++; }, []);

  const cargarFlota = useCallback(async () => {
    const consulta = ++ultimaConsulta.current;
    try {
      const data = await container.asociaciones.obtenerFlotaAsociada.execute();
      if (consulta !== ultimaConsulta.current) return;
      setFlota(data);
      setErrorFlota(null);
    } catch (e) {
      if (consulta !== ultimaConsulta.current) return;
      setErrorFlota(e instanceof Error && e.message ? e.message : "No se pudieron cargar los camiones.");
    }
  }, []);

  useEffect(() => {
    cargarFlota();
    return invalidarConsulta;
  }, [cargarFlota, invalidarConsulta]);

  const asociar = useCallback(async (transportistaId: string, camionId: string) => {
    await container.asociaciones.asociarCamion.execute(transportistaId, camionId);
    await cargarFlota();
    alCambiar();
  }, [cargarFlota, alCambiar]);

  const desasociar = useCallback(async (asociacionId: string) => {
    await container.asociaciones.desasociarCamion.execute(asociacionId);
    await cargarFlota();
    alCambiar();
  }, [cargarFlota, alCambiar]);

  return { flota, errorFlota, recargarFlota: cargarFlota, asociar, desasociar };
}

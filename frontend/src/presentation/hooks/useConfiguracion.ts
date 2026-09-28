import { useCallback, useEffect, useState } from "react";
import { container } from "../../composition/container";
import type { Configuracion } from "../../domain/entities/Configuracion";

export function useConfiguracion() {
  const [config, setConfig] = useState<Configuracion | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [probandoConexion, setProbandoConexion] = useState(false);

  useEffect(() => {
    container.configuracion.obtenerConfiguracion
      .execute()
      .then(setConfig)
      .catch(() => setError("No se pudo cargar la configuración."))
      .finally(() => setCargando(false));
  }, []);

  const guardarIngenio = useCallback(async (datos: Configuracion["ingenio"]) => {
    await container.configuracion.guardarDatosIngenio.execute(datos);
    setConfig((prev) => (prev ? { ...prev, ingenio: datos } : prev));
  }, []);

  const guardarReglas = useCallback(async (reglas: Configuracion["reglas"]) => {
    await container.configuracion.guardarReglasNegocio.execute(reglas);
    setConfig((prev) => (prev ? { ...prev, reglas } : prev));
  }, []);

  const guardarNotificaciones = useCallback(async (notificaciones: Configuracion["notificaciones"]) => {
    await container.configuracion.guardarNotificaciones.execute(notificaciones);
    setConfig((prev) => (prev ? { ...prev, notificaciones } : prev));
  }, []);

  const probarConexion = useCallback(async () => {
    setProbandoConexion(true);
    try {
      return await container.configuracion.probarConexionN8n.execute();
    } finally {
      setProbandoConexion(false);
    }
  }, []);

  return {
    config,
    cargando,
    error,
    probandoConexion,
    guardarIngenio,
    guardarReglas,
    guardarNotificaciones,
    probarConexion,
  };
}

import { useCallback, useEffect, useState } from "react";
import { container } from "../../composition/container";
import type {
  Conversacion,
  MetricasChatbot,
  PasoFlujoBot,
  PlantillaMensaje,
} from "../../domain/entities/Conversacion";

export function useChatbot() {
  const [conversaciones, setConversaciones] = useState<Conversacion[]>([]);
  const [conversacionActivaId, setConversacionActivaId] = useState<string | null>(null);
  const [metricas, setMetricas] = useState<MetricasChatbot | null>(null);
  const [flujo, setFlujo] = useState<PasoFlujoBot[]>([]);
  const [plantillas, setPlantillas] = useState<PlantillaMensaje[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let activo = true;
    setCargando(true);
    Promise.all([
      container.conversaciones.obtenerConversaciones.execute(),
      container.conversaciones.obtenerMetricasChatbot.execute(),
      container.conversaciones.obtenerFlujoDeAutomatizacion.execute(),
      container.conversaciones.obtenerPlantillas.execute(),
    ])
      .then(([convs, met, fl, plant]) => {
        if (!activo) return;
        setConversaciones(convs);
        setMetricas(met);
        setFlujo(fl);
        setPlantillas(plant);
        setConversacionActivaId(convs[0]?.id ?? null);
      })
      .catch(() => activo && setError("No se pudo cargar la información del chatbot."))
      .finally(() => activo && setCargando(false));
    return () => {
      activo = false;
    };
  }, []);

  const conversacionActiva = conversaciones.find((c) => c.id === conversacionActivaId) ?? null;

  const guardarPlantillas = useCallback(async (nuevas: PlantillaMensaje[]) => {
    await container.conversaciones.guardarPlantillas.execute(nuevas);
    setPlantillas(nuevas);
  }, []);

  return {
    conversaciones,
    conversacionActiva,
    seleccionarConversacion: setConversacionActivaId,
    metricas,
    flujo,
    plantillas,
    setPlantillas,
    guardarPlantillas,
    cargando,
    error,
  };
}

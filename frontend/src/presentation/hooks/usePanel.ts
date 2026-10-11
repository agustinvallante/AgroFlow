import { useCallback, useEffect, useState } from "react";
import { container } from "../../composition/container";
import type { FrancoTimeline, MetricasPanel } from "../../domain/entities/MetricasPanel";

export function usePanel() {
  const [metricas, setMetricas] = useState<MetricasPanel | null>(null);
  const [timeline, setTimeline] = useState<FrancoTimeline[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    try {
      const [m, t] = await Promise.all([
        container.panel.obtenerMetricasPanel.execute(),
        container.panel.obtenerTimelineDelDia.execute(),
      ]);
      setMetricas(m);
      setTimeline(t);
    } catch {
      setError("No se pudieron cargar los datos del panel.");
    } finally {
      setCargando(false);
    }
  }, []);

  useEffect(() => {
    cargar();
    const id = setInterval(() => {
      container.panel.obtenerTimelineDelDia.execute().then(setTimeline);
    }, 15000);
    return () => clearInterval(id);
  }, [cargar]);

  const alternarMolienda = useCallback(async () => {
    const nuevasMetricas = await container.panel.alternarEstadoMolienda.execute();
    setMetricas(nuevasMetricas);
    return nuevasMetricas;
  }, []);

  return { metricas, timeline, cargando, error, alternarMolienda, recargar: cargar };
}

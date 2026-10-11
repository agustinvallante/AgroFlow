import { useCallback, useEffect, useState } from "react";
import { container } from "../../composition/container";
import type { RangoReporte, Reporte } from "../../domain/entities/Reporte";

export function useReportes() {
  const [rango, setRango] = useState<RangoReporte>("semana");
  const [reporte, setReporte] = useState<Reporte | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(async (rangoActual: RangoReporte) => {
    setCargando(true);
    setError(null);
    try {
      const data = await container.reportes.obtenerReporte.execute(rangoActual);
      setReporte(data);
    } catch {
      setError("No se pudo cargar el reporte.");
    } finally {
      setCargando(false);
    }
  }, []);

  useEffect(() => {
    cargar(rango);
  }, [rango, cargar]);

  return { rango, setRango, reporte, cargando, error };
}

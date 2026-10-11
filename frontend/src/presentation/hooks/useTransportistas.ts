import { useCallback, useEffect, useMemo, useState } from "react";
import { container } from "../../composition/container";
import type { NuevoTransportista, Transportista } from "../../domain/entities/Transportista";

export type FiltroFlotaTp = "todas" | "propia" | "tercero";
export type FiltroEstadoTp = "todos" | "activo" | "inactivo";

export function useTransportistas() {
  const [transportistas, setTransportistas] = useState<Transportista[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [busqueda, setBusqueda] = useState("");
  const [filtroFlota, setFiltroFlota] = useState<FiltroFlotaTp>("todas");
  const [filtroEstado, setFiltroEstado] = useState<FiltroEstadoTp>("todos");

  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    try {
      const data = await container.transportistas.obtenerTransportistas.execute();
      setTransportistas(data);
    } catch {
      setError("No se pudo cargar el listado de transportistas.");
    } finally {
      setCargando(false);
    }
  }, []);

  useEffect(() => {
    cargar();
  }, [cargar]);

  const listaFiltrada = useMemo(() => {
    const texto = busqueda.toLowerCase().trim();
    return transportistas.filter((t) => {
      const coincideTexto =
        !texto ||
        t.nombre.toLowerCase().includes(texto) ||
        t.patente.toLowerCase().includes(texto) ||
        t.finca.toLowerCase().includes(texto);
      const coincideFlota = filtroFlota === "todas" || t.flota === filtroFlota;
      const coincideEstado =
        filtroEstado === "todos" || (filtroEstado === "activo") === t.activo;
      return coincideTexto && coincideFlota && coincideEstado;
    });
  }, [transportistas, busqueda, filtroFlota, filtroEstado]);

  const resumen = useMemo(
    () => ({
      total: transportistas.length,
      propia: transportistas.filter((t) => t.flota === "propia").length,
      tercero: transportistas.filter((t) => t.flota === "tercero").length,
      activos: transportistas.filter((t) => t.activo).length,
    }),
    [transportistas]
  );

  const crear = useCallback(async (datos: NuevoTransportista) => {
    const nuevo = await container.transportistas.crearTransportista.execute(datos);
    setTransportistas((prev) => [nuevo, ...prev]);
  }, []);

  const cambiarEstado = useCallback(async (id: string) => {
    const actualizado = await container.transportistas.cambiarEstadoActivoTransportista.execute(id);
    setTransportistas((prev) => prev.map((t) => (t.id === id ? actualizado : t)));
  }, []);

  return {
    transportistas: listaFiltrada,
    resumen,
    cargando,
    error,
    filtros: { busqueda, filtroFlota, filtroEstado },
    setBusqueda,
    setFiltroFlota,
    setFiltroEstado,
    crear,
    cambiarEstado,
    recargar: cargar,
  };
}

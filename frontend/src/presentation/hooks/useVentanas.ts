import { useCallback, useEffect, useState } from "react";
import { container } from "../../composition/container";
import type { VentanaRecepcion } from "../../domain/entities/VentanaRecepcion";

export const INTERVALO_VENTANAS_MS = 4000;

interface Lectura {
  fecha: string;
  ventanas: VentanaRecepcion[] | null;
  error: string | null;
}

export function useVentanas() {
  // Vacío significa hoy según el servidor, nunca según la zona del navegador.
  const [fecha, setFecha] = useState("");
  const [lectura, setLectura] = useState<Lectura | null>(null);
  const [revision, setRevision] = useState(0);
  const recargar = useCallback(() => setRevision((actual) => actual + 1), []);

  useEffect(() => {
    let vigente = true;
    const consulta = { pendiente: false };
    async function leer() {
      // No acumular consultas si la API tarda más que el intervalo.
      if (consulta.pendiente) return;
      consulta.pendiente = true;
      try {
        const ventanas = await container.ventanas.obtenerVentanas.execute(fecha || undefined);
        if (vigente) setLectura({ fecha, ventanas, error: null });
      } catch (e) {
        if (vigente) {
          const error = e instanceof Error && e.message ? e.message : "No se pudieron cargar las franjas.";
          setLectura((anterior) => ({
            fecha,
            ventanas: anterior?.fecha === fecha ? anterior.ventanas : null,
            error,
          }));
        }
      } finally {
        consulta.pendiente = false;
      }
    }
    void leer();
    const temporizador = setInterval(() => { void leer(); }, INTERVALO_VENTANAS_MS);
    return () => {
      vigente = false;
      clearInterval(temporizador);
    };
  }, [fecha, revision]);

  // Al cambiar la fecha, nunca presentar las franjas de la consulta anterior.
  const actual = lectura?.fecha === fecha ? lectura : null;
  return {
    fecha, setFecha, recargar,
    ventanas: actual?.ventanas ?? [],
    cargando: actual === null,
    error: actual?.error ?? null,
    desactualizado: actual?.ventanas !== null && actual?.error != null,
  };
}

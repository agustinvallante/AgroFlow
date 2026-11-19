/**
 * MEDIDAS TEMPORALES para la demo. ZafraFlow API no publica el origen de un
 * turno ni el estado de la molienda; hasta que lo haga, el panel los resuelve
 * en el navegador. Lo guardado vive sólo en este navegador y no reemplaza
 * datos del backend (ADR-003).
 */
import { useCallback, useState } from "react";

const CLAVE_MANUALES = "zafraflow.turnos-manuales.v1";
const CLAVE_MOLIENDA = "zafraflow.molienda-demo.v1";
/** Tope para que la lista no crezca sin límite. */
const MAXIMO_MANUALES = 500;

function leer(clave: string): unknown {
  try {
    return JSON.parse(localStorage.getItem(clave) ?? "null");
  } catch {
    return null;
  }
}

function guardar(clave: string, valor: unknown) {
  try {
    localStorage.setItem(clave, JSON.stringify(valor));
  } catch {
    // Sin almacenamiento la demo sigue funcionando, sin recordar.
  }
}

/**
 * Turnos cargados desde este dashboard. El bot y la carga manual usan el mismo
 * POST, así que todo turno que no figure acá se atribuye al bot.
 */
export function turnosManuales(): Set<string> {
  const guardado = leer(CLAVE_MANUALES);
  return new Set(Array.isArray(guardado) ? guardado.filter((id) => typeof id === "string") : []);
}

export function registrarTurnoManual(id: string) {
  const ids = [...turnosManuales()].filter((otro) => otro !== id);
  guardar(CLAVE_MANUALES, [...ids, id].slice(-MAXIMO_MANUALES));
}

/** Estado de la molienda simulado; arranca operando. */
export function useMoliendaDemo() {
  const [operando, setOperando] = useState(() => leer(CLAVE_MOLIENDA) !== "detenida");
  const alternar = useCallback(() => {
    guardar(CLAVE_MOLIENDA, operando ? "detenida" : "operando");
    setOperando(!operando);
  }, [operando]);
  return { operando, alternar };
}

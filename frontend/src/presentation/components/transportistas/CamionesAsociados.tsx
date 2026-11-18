import { useState } from "react";
import type { Camion, CamionAsociado } from "../../../domain/entities/Asociacion";

interface Props {
  nombreTransportista: string;
  activo: boolean;
  asociados: CamionAsociado[];
  libres: Camion[];
  onAsociar: (camionId: string) => Promise<void>;
  onDesasociar: (asociacionId: string) => Promise<void>;
}

/**
 * Celda de camiones de un transportista: lista sus camiones con la opción de
 * desasociarlos y, si está activo, permite asociar un camión libre.
 */
export function CamionesAsociados({ nombreTransportista, activo, asociados, libres, onAsociar, onDesasociar }: Props) {
  const [eligiendo, setEligiendo] = useState(false);
  const [camionId, setCamionId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  const ejecutar = async (accion: () => Promise<void>, alTerminar?: () => void) => {
    setError(null);
    setEnviando(true);
    try {
      await accion();
      alTerminar?.();
    } catch (e) {
      setError(e instanceof Error && e.message ? e.message : "No se pudo actualizar la asociación.");
    } finally {
      setEnviando(false);
    }
  };

  const confirmarDesasociar = (c: CamionAsociado) => {
    if (!window.confirm(`¿Desasociar el camión ${c.patente} de ${nombreTransportista}? No podrá pedir turnos con ese camión.`)) return;
    void ejecutar(() => onDesasociar(c.asociacionId));
  };

  return (
    <div>
      {asociados.length === 0 && <span>—</span>}
      {asociados.map((c) => (
        <div key={c.asociacionId} style={{ display: "flex", gap: 6, alignItems: "center" }}>
          <span style={{ fontFamily: "'IBM Plex Mono',monospace" }}>{c.patente}</span>
          <button
            className="btn-mini"
            disabled={enviando}
            aria-label={`Desasociar ${c.patente}`}
            onClick={() => confirmarDesasociar(c)}
          >
            Desasociar
          </button>
        </div>
      ))}

      {activo && !eligiendo && (
        <button className="btn-mini" disabled={enviando || libres.length === 0} onClick={() => setEligiendo(true)}
          title={libres.length === 0 ? "No hay camiones libres" : undefined}>
          Asociar camión
        </button>
      )}

      {activo && eligiendo && (
        <div style={{ display: "flex", gap: 6, marginTop: 4 }}>
          <select aria-label={`Camión para ${nombreTransportista}`} value={camionId} onChange={(e) => setCamionId(e.target.value)}>
            <option value="">Elegí un camión libre</option>
            {libres.map((c) => (
              <option key={c.id} value={c.id}>{c.patente} · {c.tipoFlota}</option>
            ))}
          </select>
          <button
            className="btn-primary"
            disabled={enviando}
            onClick={() => ejecutar(() => onAsociar(camionId), () => { setEligiendo(false); setCamionId(""); })}
          >
            Asociar
          </button>
          <button className="btn-mini" disabled={enviando} onClick={() => { setEligiendo(false); setCamionId(""); setError(null); }}>
            Cancelar
          </button>
        </div>
      )}

      {error && <div role="alert" className="fuente-demo">{error}</div>}
    </div>
  );
}

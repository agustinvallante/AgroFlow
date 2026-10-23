import type { Conversacion, EstadoEnvio, EstadoProcesamiento } from "../../../domain/entities/Conversacion";

const META_LABEL: Record<Conversacion["estado"], string> = {
  resuelto: "Resuelto",
  bot: "Gestionado por el bot",
  escalado: "Escalado a un operador",
  revision: "Requiere revisión de un operador",
};

// "Aceptado" es lo que informa el proveedor: no implica entregado ni leído.
const ENVIO_LABEL: Record<EstadoEnvio, string> = {
  pendiente: "Pendiente de envío",
  aceptado: "Aceptado por el proveedor",
  fallido: "Envío fallido",
  incierto: "Envío incierto · requiere revisión",
};

const PROCESAMIENTO_LABEL: Partial<Record<EstadoProcesamiento, string>> = {
  fallido: "Sin respuesta · procesamiento fallido",
  vencido: "Sin respuesta · autorización vencida",
};

interface ChatTranscriptProps {
  conversacion: Conversacion | null;
  cargando?: boolean;
  error?: string | null;
}

export function ChatTranscript({ conversacion, cargando = false, error = null }: ChatTranscriptProps) {
  const detalle = conversacion
    ? [
        conversacion.direccion && conversacion.remitenteReconocido ? conversacion.direccion : null,
        `Hoy · ${conversacion.hora}`,
        META_LABEL[conversacion.estado],
      ]
        .filter(Boolean)
        .join(" · ")
    : "Seleccioná una conversación";

  return (
    <div className="chat-transcript">
      <div className="chat-transcript-head">
        <strong>{conversacion?.nombre ?? "—"}</strong>
        <span>{detalle}</span>
      </div>
      <div className="chat-transcript-body">
        {error && <div className="error-state">{error}</div>}
        {cargando && conversacion?.mensajes.length === 0 && <div className="loading-state">Cargando mensajes...</div>}
        {conversacion?.mensajes.map((m, i) => {
          const estado = m.estadoEnvio ? ENVIO_LABEL[m.estadoEnvio] : m.procesamiento ? PROCESAMIENTO_LABEL[m.procesamiento] : undefined;
          return (
            <div className={`msg ${m.autor === "transportista" ? "in" : "out"}`} key={m.id ?? i}>
              <span className="who">
                {m.autor === "transportista" ? "Transportista" : "Bot AgroFlow"}
                {m.hora ? ` · ${m.hora}` : ""}
              </span>
              {m.texto}
              {estado && <span className={`msg-status ${m.estadoEnvio ?? m.procesamiento}`}>{estado}</span>}
            </div>
          );
        })}
      </div>
    </div>
  );
}

import type { Conversacion } from "../../../domain/entities/Conversacion";

const META_LABEL: Record<Conversacion["estado"], string> = {
  resuelto: "Resuelto",
  bot: "Gestionado por el bot",
  escalado: "Escalado a un operador",
};

export function ChatTranscript({ conversacion }: { conversacion: Conversacion | null }) {
  return (
    <div className="chat-transcript">
      <div className="chat-transcript-head">
        <strong>{conversacion?.nombre ?? "—"}</strong>
        <span>
          {conversacion ? `Hoy · ${conversacion.hora} · ${META_LABEL[conversacion.estado]}` : "Seleccioná una conversación"}
        </span>
      </div>
      <div className="chat-transcript-body">
        {conversacion?.mensajes.map((m, i) => (
          <div className={`msg ${m.autor === "transportista" ? "in" : "out"}`} key={i}>
            <span className="who">{m.autor === "transportista" ? "Transportista" : "Bot AgroFlow"}</span>
            {m.texto}
          </div>
        ))}
      </div>
    </div>
  );
}

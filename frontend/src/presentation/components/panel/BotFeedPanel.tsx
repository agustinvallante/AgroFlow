import type { MensajeChat } from "../../../domain/entities/Conversacion";

export function BotFeedPanel({ mensajes }: { mensajes: MensajeChat[] }) {
  return (
    <div className="bot-panel">
      <div className="bot-panel-head">
        <span className="status-dot" />
        <strong>Chatbot WhatsApp</strong>
        <span>conectado</span>
      </div>
      <div className="bot-log">
        {mensajes.map((m, i) => (
          <div
            className={`msg ${m.autor === "transportista" ? "in" : "out"}`}
            key={i}
          >
            <span className="who">
              {m.autor === "transportista" ? "Transportista" : "Bot AgroFlow"}
            </span>
            {m.texto}
          </div>
        ))}
      </div>
    </div>
  );
}

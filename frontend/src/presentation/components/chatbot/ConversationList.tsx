import type { Conversacion } from "../../../domain/entities/Conversacion";

const TAG_LABEL: Record<Conversacion["estado"], string> = {
  resuelto: "Resuelto",
  bot: "Con el bot",
  escalado: "Escalado",
};

interface ConversationListProps {
  conversaciones: Conversacion[];
  activaId: string | null;
  onSeleccionar: (id: string) => void;
}

export function ConversationList({ conversaciones, activaId, onSeleccionar }: ConversationListProps) {
  return (
    <div className="conv-list">
      {conversaciones.map((c) => (
        <div
          key={c.id}
          className={`conv-item ${c.id === activaId ? "active" : ""}`}
          onClick={() => onSeleccionar(c.id)}
        >
          <div className="conv-top">
            <span>{c.nombre}</span>
            <span className="conv-time">{c.hora}</span>
          </div>
          <div className="conv-prev">{c.preview}</div>
          <span className={`conv-tag ${c.estado}`}>{TAG_LABEL[c.estado]}</span>
        </div>
      ))}
    </div>
  );
}

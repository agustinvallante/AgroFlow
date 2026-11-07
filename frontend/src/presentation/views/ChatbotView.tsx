import { MetricCard } from "../components/common/MetricCard";
import { EmptyState } from "../components/common/StatusStates";
import { AVISO_SIN_API, SIN_DATO } from "../components/common/sinDatos";
import { ChatTranscript } from "../components/chatbot/ChatTranscript";
import { ConversationList } from "../components/chatbot/ConversationList";

export function ChatbotView() {
  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Chatbot de WhatsApp</h1>
          <div className="zafra">{AVISO_SIN_API}</div>
        </div>
      </div>

      <div className="metrics" style={{ marginBottom: 22 }}>
        <MetricCard label="Mensajes hoy" value={SIN_DATO} />
        <MetricCard label="Turnos asignados por el bot" value={SIN_DATO} />
        <MetricCard label="Tiempo promedio de respuesta" value={SIN_DATO} />
        <MetricCard label="Escaladas a un operador" value={SIN_DATO} />
      </div>

      <section>
        <div className="section-head">
          <h2>Conversaciones recientes</h2>
        </div>
        <div className="chat-layout">
          <ConversationList conversaciones={[]} activaId={null} onSeleccionar={() => {}} />
          <ChatTranscript conversacion={null} />
        </div>
      </section>

      <section>
        <div className="section-head">
          <h2>Plantillas de mensajes</h2>
          <div className="meta">Usadas por el bot en cada paso del flujo</div>
        </div>
        <EmptyState message="Sin plantillas." />
        <button className="btn-primary" style={{ marginTop: 16 }} disabled>
          Guardar plantillas
        </button>
      </section>
    </div>
  );
}

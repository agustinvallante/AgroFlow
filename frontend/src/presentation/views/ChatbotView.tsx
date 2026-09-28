import { useState } from "react";
import { useChatbot } from "../hooks/useChatbot";
import { MetricCard } from "../components/common/MetricCard";
import { ConversationList } from "../components/chatbot/ConversationList";
import { ChatTranscript } from "../components/chatbot/ChatTranscript";
import { ErrorState, LoadingState } from "../components/common/StatusStates";

export function ChatbotView() {
  const {
    conversaciones,
    conversacionActiva,
    seleccionarConversacion,
    metricas,
    plantillas,
    setPlantillas,
    guardarPlantillas,
    cargando,
    error,
  } = useChatbot();

  const [guardado, setGuardado] = useState(false);

  const actualizarPlantilla = (id: string, contenido: string) => {
    setPlantillas(
      plantillas.map((p) => (p.id === id ? { ...p, contenido } : p)),
    );
  };

  const handleGuardar = async () => {
    await guardarPlantillas(plantillas);
    setGuardado(true);
    setTimeout(() => setGuardado(false), 2500);
  };

  if (error) return <ErrorState message={error} />;
  if (cargando) return <LoadingState label="Cargando el chatbot..." />;

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Chatbot de WhatsApp</h1>
          <div className="zafra">
            Automatización de turnos vía n8n · Ingenio San Ramón
          </div>
        </div>
        <div className="conn-status">
          <i />
          Conectado a WhatsApp Business API
        </div>
      </div>

      {metricas && (
        <div className="metrics" style={{ marginBottom: 22 }}>
          <MetricCard label="Mensajes hoy" value={metricas.mensajesHoy} />
          <MetricCard
            label="Turnos asignados por el bot"
            value={metricas.turnosAsignadosPorBot}
          />
          <MetricCard
            label="Tiempo promedio de respuesta"
            value={metricas.tiempoPromedioRespuestaSeg}
            unit="seg"
          />
          <MetricCard
            label="Escaladas a un operador"
            value={metricas.conversacionesEscaladas}
            valueColor="var(--alerta)"
          />
        </div>
      )}

      <section>
        <div className="section-head">
          <h2>Conversaciones recientes</h2>
          <div className="meta">Últimas 24 horas</div>
        </div>
        <div className="chat-layout">
          <ConversationList
            conversaciones={conversaciones}
            activaId={conversacionActiva?.id ?? null}
            onSeleccionar={seleccionarConversacion}
          />
          <ChatTranscript conversacion={conversacionActiva} />
        </div>
      </section>

      <section>
        <div className="section-head">
          <h2>Plantillas de mensajes</h2>
          <div className="meta">Usadas por el bot en cada paso del flujo</div>
        </div>
        <div className="template-grid">
          {plantillas.map((p) => (
            <div className="template-card" key={p.id}>
              <h4>{p.titulo}</h4>
              <textarea
                value={p.contenido}
                onChange={(e) => actualizarPlantilla(p.id, e.target.value)}
              />
            </div>
          ))}
        </div>
        <button
          className="btn-primary"
          style={{ marginTop: 16 }}
          onClick={handleGuardar}
        >
          Guardar plantillas
        </button>
        {guardado && (
          <p style={{ color: "var(--ok)", marginTop: 8 }}>
            Plantillas guardadas.
          </p>
        )}
      </section>
    </div>
  );
}

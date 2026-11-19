import { useState } from "react";
import type { Conversacion } from "../../../domain/entities/Conversacion";
import { AvatarConversacion } from "./AvatarConversacion";

const TAG_LABEL: Record<Conversacion["estado"], string> = {
  resuelto: "Resuelto",
  bot: "Con el bot",
  escalado: "Escalado",
  revision: "Requiere revisión",
};

type Filtro = "todas" | "bot" | "revision" | "no-registrado";

const FILTROS: { id: Filtro; label: string; incluye: (c: Conversacion) => boolean }[] = [
  { id: "todas", label: "Todas", incluye: () => true },
  { id: "bot", label: "Con el bot", incluye: (c) => c.estado === "bot" },
  { id: "revision", label: "Requiere revisión", incluye: (c) => c.estado === "revision" },
  { id: "no-registrado", label: "No registrado", incluye: (c) => c.remitenteReconocido === false },
];

/** Sin mayúsculas ni tildes: "agustin" encuentra "Agustín". */
const normalizar = (texto: string) => texto.normalize("NFD").replace(/\p{M}/gu, "").toLocaleLowerCase("es");

const SIN_IDS: ReadonlySet<string> = new Set();

interface ConversationListProps {
  conversaciones: Conversacion[];
  activaId: string | null;
  onSeleccionar: (id: string) => void;
  /** Novedades sin ver por conversación, contadas en esta sesión. */
  noLeidos?: Readonly<Record<string, number>>;
  /** Conversaciones con actividad en el último refresco. */
  destacadas?: ReadonlySet<string>;
}

export function ConversationList({
  conversaciones,
  activaId,
  onSeleccionar,
  noLeidos = {},
  destacadas = SIN_IDS,
}: ConversationListProps) {
  const [busqueda, setBusqueda] = useState("");
  const [filtro, setFiltro] = useState<Filtro>("todas");
  const consulta = normalizar(busqueda.trim());
  const { incluye } = FILTROS.find((f) => f.id === filtro)!;
  // Conserva el orden de la API.
  const visibles = conversaciones.filter(
    (c) => incluye(c) && (!consulta || normalizar(c.nombre).includes(consulta) || normalizar(c.preview).includes(consulta)),
  );

  return (
    <aside className="chat-convs">
      <div className="chat-convs-filtros">
        <label htmlFor="chat-buscar">Buscar conversación</label>
        <div className="chat-buscar">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <circle cx="11" cy="11" r="7" />
            <path d="m20 20-3.5-3.5" />
          </svg>
          <input id="chat-buscar" type="search" value={busqueda} placeholder="Nombre o mensaje"
            onChange={(e) => setBusqueda(e.target.value)} />
        </div>
        <div className="chat-chips" role="group" aria-label="Filtrar conversaciones">
          {FILTROS.map((f) => (
            <button key={f.id} type="button" className={`chat-chip ${f.id === filtro ? "activo" : ""}`}
              aria-pressed={f.id === filtro} onClick={() => setFiltro(f.id)}>
              {f.label}
            </button>
          ))}
        </div>
      </div>
      <div className="chat-convs-lista">
        {conversaciones.length === 0 && <div className="empty-state">Todavía no hay conversaciones registradas.</div>}
        {conversaciones.length > 0 && visibles.length === 0 && (
          <p className="chat-convs-vacio">Ninguna conversación coincide con la búsqueda.</p>
        )}
        {visibles.map((c) => {
          const activa = c.id === activaId;
          const pendientes = activa ? 0 : noLeidos[c.id] ?? 0;
          const clases = ["chat-conv", activa ? "activa" : "", pendientes > 0 ? "no-leida" : "",
            !activa && destacadas.has(c.id) ? "destacada" : ""];
          return (
            <button key={c.id} type="button" className={clases.filter(Boolean).join(" ")}
              aria-current={activa ? "true" : undefined} onClick={() => onSeleccionar(c.id)}>
              <AvatarConversacion conversacion={c} />
              <span className="chat-conv-cuerpo">
                <span className="chat-conv-fila">
                  <span className="chat-conv-nombre">{c.nombre}</span>
                  <span className="chat-conv-hora">{c.hora}</span>
                </span>
                <span className="chat-conv-fila">
                  <span className="chat-conv-preview">{c.preview}</span>
                  {pendientes > 0 && (
                    <span className="chat-badge"
                      aria-label={pendientes === 1 ? "1 novedad sin leer" : `${pendientes} novedades sin leer`}>
                      {pendientes}
                    </span>
                  )}
                </span>
                <span className="chat-conv-tags">
                  <span className={`chat-tag ${c.estado}`}>{TAG_LABEL[c.estado]}</span>
                  {c.remitenteReconocido === false && <span className="chat-tag no-registrado">No registrado</span>}
                </span>
              </span>
            </button>
          );
        })}
      </div>
    </aside>
  );
}

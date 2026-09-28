import { useState } from "react";
import { useColaTurnos } from "../hooks/useColaTurnos";
import { MetricCard } from "../components/common/MetricCard";
import { CanalBadge, EstadoBadge, FlotaBadge, PrioridadBadge } from "../components/common/Badges";
import { EmptyState, ErrorState, LoadingState } from "../components/common/StatusStates";
import { calcularPrioridadCorte, type FlotaTipo } from "../../domain/entities/Turno";

const ESTADO_OPCIONES: { value: string; label: string }[] = [
  { value: "todos", label: "Todos los estados" },
  { value: "pendiente", label: "Pendiente" },
  { value: "viaje", label: "En viaje" },
  { value: "cancha", label: "En canchón" },
  { value: "descargando", label: "Descargando" },
  { value: "completado", label: "Completado" },
  { value: "demorado", label: "Demorado" },
];

export function ColaTurnosView() {
  const {
    turnos,
    resumen,
    cargando,
    error,
    filtros,
    setBusqueda,
    setFiltroFlota,
    setFiltroEstado,
    setOrden,
    crearTurno,
    reasignar,
    cancelar,
  } = useColaTurnos();

  const [mostrarForm, setMostrarForm] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [form, setForm] = useState({
    patente: "",
    chofer: "",
    finca: "",
    flota: "propia" as FlotaTipo,
    hora: "",
    horasDesdeCorte: "",
  });

  const actualizarCampo = (campo: keyof typeof form, valor: string) => {
    setForm((prev) => ({ ...prev, [campo]: valor }));
  };

  const guardarTurno = async () => {
    setFormError(null);
    try {
      await crearTurno({
        patente: form.patente,
        chofer: form.chofer,
        finca: form.finca,
        flota: form.flota,
        hora: form.hora,
        horasDesdeCorte: Number(form.horasDesdeCorte) || 0,
      });
      setForm({ patente: "", chofer: "", finca: "", flota: "propia", hora: "", horasDesdeCorte: "" });
      setMostrarForm(false);
    } catch (e) {
      setFormError(e instanceof Error ? e.message : "No se pudo crear el turno.");
    }
  };

  const handleReasignar = (id: string, horaActual: string) => {
    const nuevaHora = window.prompt("Nueva hora de turno (formato HH:MM):", horaActual);
    if (nuevaHora) reasignar(id, nuevaHora);
  };

  if (error) return <ErrorState message={error} />;

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Cola de turnos</h1>
          <div className="zafra">Todos los turnos programados hoy · Ingenio San Ramón</div>
        </div>
        <button className="btn-primary" onClick={() => setMostrarForm((v) => !v)}>
          + Nuevo turno
        </button>
      </div>

      <div className="metrics" style={{ marginBottom: 20 }}>
        <MetricCard label="Turnos hoy" value={resumen.total} />
        <MetricCard label="Pendientes" value={resumen.pendientes} />
        <MetricCard label="Completados" value={resumen.completados} />
        <MetricCard label="Demorados" value={resumen.demorados} valueColor="var(--alerta)" />
      </div>

      {mostrarForm && (
        <div className="alert-card" style={{ marginBottom: 22, maxWidth: 640 }}>
          <h3>Cargar turno manual</h3>
          <p>Para transportistas sin acceso a WhatsApp o casos excepcionales.</p>
          {formError && <ErrorState message={formError} />}
          <div className="form-grid">
            <div className="form-field">
              <label>Patente</label>
              <input value={form.patente} onChange={(e) => actualizarCampo("patente", e.target.value)} placeholder="AB123CD" />
            </div>
            <div className="form-field">
              <label>Transportista</label>
              <input value={form.chofer} onChange={(e) => actualizarCampo("chofer", e.target.value)} placeholder="Nombre y apellido" />
            </div>
            <div className="form-field">
              <label>Finca / Origen</label>
              <input value={form.finca} onChange={(e) => actualizarCampo("finca", e.target.value)} placeholder="Finca La Esperanza" />
            </div>
            <div className="form-field">
              <label>Flota</label>
              <select value={form.flota} onChange={(e) => actualizarCampo("flota", e.target.value)}>
                <option value="propia">Propia</option>
                <option value="tercero">Tercerizada</option>
              </select>
            </div>
            <div className="form-field">
              <label>Hora del turno</label>
              <input type="time" value={form.hora} onChange={(e) => actualizarCampo("hora", e.target.value)} />
            </div>
            <div className="form-field">
              <label>Horas desde el corte</label>
              <input type="number" min={0} value={form.horasDesdeCorte} onChange={(e) => actualizarCampo("horasDesdeCorte", e.target.value)} placeholder="12" />
            </div>
          </div>
          <div style={{ display: "flex", gap: 10, marginTop: 6 }}>
            <button className="btn-primary" onClick={guardarTurno}>Guardar turno</button>
            <button className="btn-mini" onClick={() => setMostrarForm(false)}>Cancelar</button>
          </div>
        </div>
      )}

      <div className="filters-bar">
        <input
          type="text"
          placeholder="Buscar por patente, chofer o finca..."
          value={filtros.busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
        />
        <select value={filtros.filtroFlota} onChange={(e) => setFiltroFlota(e.target.value as any)}>
          <option value="todas">Toda la flota</option>
          <option value="propia">Propia</option>
          <option value="tercero">Tercerizada</option>
        </select>
        <select value={filtros.filtroEstado} onChange={(e) => setFiltroEstado(e.target.value as any)}>
          {ESTADO_OPCIONES.map((o) => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
        <select value={filtros.orden} onChange={(e) => setOrden(e.target.value as any)}>
          <option value="turno">Ordenar por turno</option>
          <option value="prioridad">Ordenar por prioridad de corte</option>
          <option value="espera">Ordenar por espera</option>
        </select>
      </div>

      {cargando ? (
        <LoadingState label="Cargando turnos..." />
      ) : (
        <>
          <table>
            <thead>
              <tr>
                <th>Turno</th>
                <th>Patente</th>
                <th>Transportista</th>
                <th>Finca / Origen</th>
                <th>Flota</th>
                <th>Corte</th>
                <th>Prioridad</th>
                <th>Estado</th>
                <th>Canal</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {turnos.map((t) => (
                <tr key={t.id}>
                  <td style={{ fontFamily: "'IBM Plex Mono',monospace", fontSize: 12.5 }}>{t.hora}</td>
                  <td style={{ fontFamily: "'IBM Plex Mono',monospace" }}>{t.patente}</td>
                  <td>{t.chofer}</td>
                  <td>{t.finca}</td>
                  <td><FlotaBadge flota={t.flota} /></td>
                  <td style={{ fontSize: 12.5 }}>{t.horasDesdeCorte} h</td>
                  <td><PrioridadBadge prioridad={calcularPrioridadCorte(t.horasDesdeCorte)} /></td>
                  <td><EstadoBadge estado={t.estado} /></td>
                  <td><CanalBadge canal={t.canal} /></td>
                  <td>
                    <div className="row-actions">
                      <button className="btn-mini" onClick={() => handleReasignar(t.id, t.hora)}>Reasignar</button>
                      <button className="btn-mini" onClick={() => cancelar(t.id)}>Cancelar</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {turnos.length === 0 && <EmptyState message="No hay turnos que coincidan con estos filtros." />}
        </>
      )}
    </div>
  );
}

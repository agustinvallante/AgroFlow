import { useState } from "react";
import { useTransportistas } from "../hooks/useTransportistas";
import { MetricCard } from "../components/common/MetricCard";
import { ActivoBadge, FlotaBadge } from "../components/common/Badges";
import { EmptyState, ErrorState, LoadingState } from "../components/common/StatusStates";
import type { FlotaTipo } from "../../domain/entities/Turno";

export function TransportistasView() {
  const {
    transportistas,
    resumen,
    cargando,
    error,
    filtros,
    setBusqueda,
    setFiltroFlota,
    setFiltroEstado,
    crear,
    cambiarEstado,
  } = useTransportistas();

  const [mostrarForm, setMostrarForm] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [form, setForm] = useState({
    nombre: "",
    patente: "",
    telefono: "",
    finca: "",
    flota: "propia" as FlotaTipo,
  });

  const actualizarCampo = (campo: keyof typeof form, valor: string) => {
    setForm((prev) => ({ ...prev, [campo]: valor }));
  };

  const guardar = async () => {
    setFormError(null);
    try {
      await crear(form);
      setForm({ nombre: "", patente: "", telefono: "", finca: "", flota: "propia" });
      setMostrarForm(false);
    } catch (e) {
      setFormError(e instanceof Error ? e.message : "No se pudo crear el transportista.");
    }
  };

  if (error) return <ErrorState message={error} />;

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Transportistas</h1>
          <div className="zafra">Flota propia y tercerizada · Ingenio San Ramón</div>
        </div>
        <button className="btn-primary" onClick={() => setMostrarForm((v) => !v)}>
          + Nuevo transportista
        </button>
      </div>

      <div className="metrics" style={{ marginBottom: 20 }}>
        <MetricCard label="Total transportistas" value={resumen.total} />
        <MetricCard label="Flota propia" value={resumen.propia} />
        <MetricCard label="Flota tercerizada" value={resumen.tercero} />
        <MetricCard label="Activos hoy" value={resumen.activos} />
      </div>

      {mostrarForm && (
        <div className="alert-card" style={{ marginBottom: 22, maxWidth: 640 }}>
          <h3>Nuevo transportista</h3>
          {formError && <ErrorState message={formError} />}
          <div className="form-grid">
            <div className="form-field">
              <label>Nombre y apellido</label>
              <input value={form.nombre} onChange={(e) => actualizarCampo("nombre", e.target.value)} placeholder="Nombre completo" />
            </div>
            <div className="form-field">
              <label>Patente</label>
              <input value={form.patente} onChange={(e) => actualizarCampo("patente", e.target.value)} placeholder="AB123CD" />
            </div>
            <div className="form-field">
              <label>Teléfono (WhatsApp)</label>
              <input value={form.telefono} onChange={(e) => actualizarCampo("telefono", e.target.value)} placeholder="+54 381 000 0000" />
            </div>
            <div className="form-field">
              <label>Finca habitual</label>
              <input value={form.finca} onChange={(e) => actualizarCampo("finca", e.target.value)} placeholder="Finca La Esperanza" />
            </div>
            <div className="form-field">
              <label>Flota</label>
              <select value={form.flota} onChange={(e) => actualizarCampo("flota", e.target.value)}>
                <option value="propia">Propia</option>
                <option value="tercero">Tercerizada</option>
              </select>
            </div>
          </div>
          <div style={{ display: "flex", gap: 10, marginTop: 6 }}>
            <button className="btn-primary" onClick={guardar}>Guardar transportista</button>
            <button className="btn-mini" onClick={() => setMostrarForm(false)}>Cancelar</button>
          </div>
        </div>
      )}

      <div className="filters-bar">
        <input
          type="text"
          placeholder="Buscar por nombre, patente o finca..."
          value={filtros.busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
        />
        <select value={filtros.filtroFlota} onChange={(e) => setFiltroFlota(e.target.value as any)}>
          <option value="todas">Toda la flota</option>
          <option value="propia">Propia</option>
          <option value="tercero">Tercerizada</option>
        </select>
        <select value={filtros.filtroEstado} onChange={(e) => setFiltroEstado(e.target.value as any)}>
          <option value="todos">Todos</option>
          <option value="activo">Activos</option>
          <option value="inactivo">Inactivos</option>
        </select>
      </div>

      {cargando ? (
        <LoadingState label="Cargando transportistas..." />
      ) : (
        <>
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Patente</th>
                <th>Teléfono</th>
                <th>Finca habitual</th>
                <th>Flota</th>
                <th>Turnos este mes</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {transportistas.map((t) => (
                <tr key={t.id}>
                  <td>{t.nombre}</td>
                  <td style={{ fontFamily: "'IBM Plex Mono',monospace" }}>{t.patente}</td>
                  <td style={{ fontFamily: "'IBM Plex Mono',monospace", fontSize: 12.5 }}>{t.telefono}</td>
                  <td>{t.finca}</td>
                  <td><FlotaBadge flota={t.flota} /></td>
                  <td>{t.turnosEsteMes}</td>
                  <td><ActivoBadge activo={t.activo} /></td>
                  <td>
                    <div className="row-actions">
                      <button className="btn-mini" onClick={() => alert(`Historial de ${t.nombre} (simulado).`)}>Historial</button>
                      <button className="btn-mini" onClick={() => cambiarEstado(t.id)}>
                        {t.activo ? "Dar de baja" : "Reactivar"}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {transportistas.length === 0 && <EmptyState message="No hay transportistas que coincidan con estos filtros." />}
        </>
      )}
    </div>
  );
}

import { useEffect, useState } from "react";
import { container } from "../../composition/container";
import { ventanaDeRfc3339 } from "../../shared/utils/date";
import { EmptyState, ErrorState, LoadingState } from "../components/common/StatusStates";
import { useVentanas } from "../hooks/useVentanas";

type Ingenio = Awaited<ReturnType<typeof container.ingenio.obtener>>;

export function ConfiguracionView() {
  const franjas = useVentanas();
  const [ingenio, setIngenio] = useState<Ingenio | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    container.ingenio.obtener().then(
      (datos) => { if (vigente) setIngenio(datos); },
      (fallo: unknown) => {
        if (vigente) setError(fallo instanceof Error ? fallo.message : "No se pudo consultar el ingenio.");
      },
    );
    return () => { vigente = false; };
  }, [revision]);

  function actualizar() {
    setError(null);
    setIngenio(null);
    setRevision((actual) => actual + 1);
    franjas.recargar();
  }

  return (
    <div className="view">
      <div className="topbar">
        <div>
          <h1>Configuración</h1>
          <div className="zafra">Configuración de recepción · Solo lectura</div>
        </div>
      </div>

      <div className="config-grid">
        <section className="config-card" aria-labelledby="config-ingenio">
          <h2 id="config-ingenio">Datos del ingenio</h2>
          {error
            ? <ErrorState message={error} />
            : ingenio
              ? (
                <dl className="config-datos">
                  <div><dt>Nombre</dt><dd>{ingenio.name}</dd></div>
                  <div><dt>Zona horaria</dt><dd>{ingenio.timeZoneId}</dd></div>
                </dl>
              )
              : <LoadingState label="Cargando datos del ingenio..." />}
        </section>

        <section className="config-card" aria-labelledby="config-franjas">
          <h2 id="config-franjas">Franjas configuradas</h2>
          <div className="franjas-filtros">
            <label>Fecha de franjas <input type="date" value={franjas.fecha} onChange={(e) => franjas.setFecha(e.target.value)} /></label>
            <button className="btn-mini" onClick={() => franjas.setFecha("")}>Hoy del ingenio</button>
            <button className="btn-mini" onClick={actualizar}>Actualizar configuración</button>
          </div>
          {franjas.cargando && <LoadingState label="Cargando franjas..." />}
          {franjas.error && (franjas.desactualizado
            ? <div className="aviso-desactualizado" role="alert">Franjas desactualizadas. {franjas.error}</div>
            : <ErrorState message={franjas.error} />)}
          {!franjas.cargando && !franjas.error && franjas.ventanas.length === 0 &&
            <EmptyState message="No hay franjas configuradas para esta fecha." />}
          {franjas.ventanas.length > 0 && (
            <table aria-label="Franjas efectivas de recepción" className={franjas.desactualizado ? "datos-desactualizados" : undefined}>
              <thead>
                <tr><th scope="col">Horario</th><th scope="col">Duración (min)</th><th scope="col">Capacidad (camiones)</th></tr>
              </thead>
              <tbody>
                {franjas.ventanas.map((ventana) => (
                  <tr key={ventana.id}>
                    <td>{ventanaDeRfc3339(ventana.startAt, ventana.endAt)}</td>
                    <td>{(Date.parse(ventana.endAt) - Date.parse(ventana.startAt)) / 60_000}</td>
                    <td>{ventana.capacity}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      </div>
    </div>
  );
}

import type { FrancoTimeline } from "../../../domain/entities/MetricasPanel";

export function Timeline({ slots }: { slots: FrancoTimeline[] }) {
  return (
    <div className="timeline-wrap">
      <div className="timeline">
        {slots.map((slot) => (
          <div className="slot" key={slot.hora}>
            <div className="slot-label">{slot.hora}</div>
            <div className="trucks">
              {Array.from({ length: slot.camionesPropia }).map((_, i) => (
                <div className="truck-tick propia" key={`p-${i}`} />
              ))}
              {Array.from({ length: slot.camionesTercero }).map((_, i) => (
                <div className="truck-tick tercero" key={`t-${i}`} />
              ))}
              {Array.from({ length: slot.camionesDemorados }).map((_, i) => (
                <div className="truck-tick demorado" key={`d-${i}`} />
              ))}
            </div>
            {slot.esActual && <div className="now-line" />}
          </div>
        ))}
      </div>
      <div className="legend">
        <span>
          <i style={{ background: "var(--azucar)" }} />
          Flota propia
        </span>
        <span>
          <i style={{ background: "var(--tinta-suave)" }} />
          Flota tercerizada
        </span>
        <span>
          <i style={{ background: "var(--alerta)" }} />
          Demorado
        </span>
      </div>
    </div>
  );
}

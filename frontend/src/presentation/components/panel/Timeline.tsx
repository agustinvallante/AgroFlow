import type { VentanaRecepcion } from "../../../domain/entities/VentanaRecepcion";
import { ventanaDeRfc3339 } from "../../../shared/utils/date";

export function Timeline({ slots }: { slots: VentanaRecepcion[] }) {
  return (
    <ul className="timeline" aria-label="Franjas y cupos">
      {slots.map((slot) => (
        <li className="slot" key={slot.id} aria-label={ventanaDeRfc3339(slot.startAt, slot.endAt)}>
          <div className="slot-label">{ventanaDeRfc3339(slot.startAt, slot.endAt)}</div>
          <span className={slot.available === 0 ? "slot-lleno" : "slot-disponible"}>
            {slot.available === 0 ? "Sin cupos" : "Con cupos"}
          </span>
          <dl className="slot-cupos">
            <div><dt>Capacidad</dt><dd>{slot.capacity}</dd></div>
            <div><dt>Ocupados</dt><dd>{slot.occupied}</dd></div>
            <div><dt>Disponibles</dt><dd>{slot.available}</dd></div>
          </dl>
        </li>
      ))}
    </ul>
  );
}

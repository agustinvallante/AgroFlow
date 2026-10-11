import { useEffect, useState } from "react";
import { fechaLarga, horaActual } from "../../shared/utils/date";

export function useClock() {
  const [hora, setHora] = useState(horaActual());
  const [fecha] = useState(fechaLarga());

  useEffect(() => {
    const id = setInterval(() => setHora(horaActual()), 1000);
    return () => clearInterval(id);
  }, []);

  return { hora, fecha };
}

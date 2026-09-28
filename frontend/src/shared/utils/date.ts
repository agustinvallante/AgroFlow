export function horaConOffset(offsetMin: number): string {
  const d = new Date();
  d.setMinutes(d.getMinutes() + offsetMin);
  return d.toTimeString().slice(0, 5);
}

export function horaActual(): string {
  return new Date().toTimeString().slice(0, 8);
}

export function fechaLarga(): string {
  return new Date().toLocaleDateString("es-AR", {
    weekday: "long",
    day: "numeric",
    month: "long",
  });
}

export function generarId(prefijo: string): string {
  return `${prefijo}-${Date.now()}-${Math.floor(Math.random() * 10000)}`;
}

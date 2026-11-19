import type { Conversacion } from "../../../domain/entities/Conversacion";

/** Iniciales del transportista reconocido; "?" para un remitente no registrado. */
function iniciales({ nombre, remitenteReconocido }: Conversacion): string {
  if (remitenteReconocido === false) return "?";
  const palabras = nombre.split(/\s+/).filter((p) => /\p{L}/u.test(p));
  const letras = palabras.length > 1 ? palabras[0][0] + palabras[1][0] : (palabras[0] ?? "?").slice(0, 2);
  return letras.toLocaleUpperCase("es");
}

interface AvatarConversacionProps {
  conversacion: Conversacion;
  grande?: boolean;
}

export function AvatarConversacion({ conversacion, grande = false }: AvatarConversacionProps) {
  const clases = ["chat-avatar", conversacion.remitenteReconocido === false ? "no-registrado" : "", grande ? "grande" : ""];
  return (
    <span className={clases.filter(Boolean).join(" ")} aria-hidden="true">
      {iniciales(conversacion)}
    </span>
  );
}

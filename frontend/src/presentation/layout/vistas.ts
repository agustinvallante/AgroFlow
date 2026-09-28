export type VistaId =
  | "general"
  | "cola"
  | "chatbot"
  | "transportistas"
  | "reportes"
  | "config";

export const VISTAS: { id: VistaId; label: string }[] = [
  { id: "general", label: "Panel general" },
  { id: "cola", label: "Cola de turnos" },
  { id: "chatbot", label: "Chatbot WhatsApp" },
  { id: "transportistas", label: "Transportistas" },
  { id: "reportes", label: "Reportes" },
  { id: "config", label: "Configuración" },
];

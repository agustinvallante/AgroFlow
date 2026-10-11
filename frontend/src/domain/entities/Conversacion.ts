export type EstadoConversacion = "resuelto" | "bot" | "escalado";

export interface MensajeChat {
  autor: "transportista" | "bot";
  texto: string;
}

export interface Conversacion {
  id: string;
  nombre: string;
  hora: string;
  preview: string;
  estado: EstadoConversacion;
  mensajes: MensajeChat[];
}

export interface PasoFlujoBot {
  orden: number;
  titulo: string;
  descripcion: string;
}

export interface PlantillaMensaje {
  id: string;
  titulo: string;
  contenido: string;
}

export interface MetricasChatbot {
  mensajesHoy: number;
  turnosAsignadosPorBot: number;
  tiempoPromedioRespuestaSeg: number;
  conversacionesEscaladas: number;
}

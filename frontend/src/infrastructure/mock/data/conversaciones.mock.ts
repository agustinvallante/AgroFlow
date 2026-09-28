import type {
  Conversacion,
  PasoFlujoBot,
  PlantillaMensaje,
} from "../../../domain/entities/Conversacion";

export function generarConversacionesSemilla(): Conversacion[] {
  return [
    {
      id: "conv-1",
      nombre: "R. Aguirre · AB213CD",
      hora: "08:14",
      preview: "Perfecto, gracias!",
      estado: "resuelto",
      mensajes: [
        { autor: "transportista", texto: "Hola, soy Ramiro, patente AB213CD, vengo de Finca La Esperanza" },
        { autor: "bot", texto: "Turno asignado: 14:30–15:00. Ingreso por Portón 2. Te avisamos si hay demoras." },
        { autor: "transportista", texto: "Perfecto, gracias!" },
      ],
    },
    {
      id: "conv-2",
      nombre: "Toledo Hnos. · AB884CD",
      hora: "08:41",
      preview: "Turno asignado: 15:00–15:30...",
      estado: "bot",
      mensajes: [
        { autor: "transportista", texto: "Buenas, camión de Toledo Hnos, finca El Retiro, cuánto tengo que esperar" },
        { autor: "bot", texto: "Turno asignado: 15:00–15:30. Flota: tercerizada. Prioridad por corte: normal." },
      ],
    },
    {
      id: "conv-3",
      nombre: "M. Correa · AB552CD",
      hora: "09:02",
      preview: "Nuevo turno: 14:00–14:30",
      estado: "bot",
      mensajes: [
        { autor: "transportista", texto: "La caña se cortó ayer a la tarde, se puede adelantar el turno?" },
        { autor: "bot", texto: "Registrado corte hace 18h. Prioridad ajustada. Nuevo turno: 14:00–14:30." },
      ],
    },
    {
      id: "conv-4",
      nombre: "J. Salazar · AB119CD",
      hora: "09:20",
      preview: "Te derivo con un operador humano.",
      estado: "escalado",
      mensajes: [
        { autor: "transportista", texto: "Mi patente no me la reconoce el sistema, la cambié la semana pasada" },
        { autor: "bot", texto: "No pude validar tu patente en el sistema. Te derivo con un operador humano." },
      ],
    },
    {
      id: "conv-5",
      nombre: "L. Farías · AB733CD",
      hora: "09:47",
      preview: "Turno reservado hasta las 16:00",
      estado: "resuelto",
      mensajes: [
        { autor: "transportista", texto: "Sigo en la finca, todavía no salgo" },
        { autor: "bot", texto: "Turno reservado hasta las 16:00. Avisanos apenas salgas para confirmar tu ventana." },
      ],
    },
    {
      id: "conv-6",
      nombre: "D. Ibáñez · AB301CD",
      hora: "10:05",
      preview: "Dirigite al canchón norte, fila 3.",
      estado: "bot",
      mensajes: [
        { autor: "transportista", texto: "Ya llegué al ingenio, dónde me ubico?" },
        { autor: "bot", texto: "Dirigite al canchón norte, fila 3. Tu turno 14:30 está confirmado." },
      ],
    },
  ];
}

export function generarFlujoBotSemilla(): PasoFlujoBot[] {
  return [
    {
      orden: 1,
      titulo: "Webhook recibido",
      descripcion: "n8n recibe el mensaje entrante desde la API de WhatsApp Business.",
    },
    {
      orden: 2,
      titulo: "Normalización de datos",
      descripcion: "Se limpia y valida la patente y el nombre de la finca informados por el chofer.",
    },
    {
      orden: 3,
      titulo: "Consulta al backend",
      descripcion: "n8n pide un turno disponible según flota, prioridad de corte y ocupación actual.",
    },
    {
      orden: 4,
      titulo: "Asignación de turno",
      descripcion: "El backend confirma la ventana horaria y el portón de ingreso asignado.",
    },
    {
      orden: 5,
      titulo: "Respuesta al transportista",
      descripcion: "n8n envía la confirmación por WhatsApp, o escala a un operador si hay un error.",
    },
  ];
}

export function generarPlantillasSemilla(): PlantillaMensaje[] {
  return [
    {
      id: "plantilla-bienvenida",
      titulo: "Bienvenida",
      contenido:
        "Hola, soy el asistente de turnos de AgroFlow. Contame tu patente y la finca de origen para asignarte un turno.",
    },
    {
      id: "plantilla-turno-asignado",
      titulo: "Turno asignado",
      contenido:
        "Turno asignado: {hora_inicio}–{hora_fin}. Ingreso por {porton}. Te avisamos si hay demoras.",
    },
    {
      id: "plantilla-demora",
      titulo: "Aviso de demora",
      contenido:
        "Hay una demora en el ingreso. Por favor esperá en la finca, te confirmamos tu nuevo turno en breve.",
    },
    {
      id: "plantilla-molienda-detenida",
      titulo: "Molienda detenida",
      contenido: "Atención: la molienda está detenida. No salgas de la finca hasta nuevo aviso.",
    },
  ];
}

/**
 * Cliente HTTP de bajo nivel. Centraliza la URL base y la traducción de una
 * respuesta de error (application/problem+json) a un ApiError con un mensaje
 * que se puede mostrar tal cual al operador.
 *
 * Turnos no tienen autenticación en la demo local. Conversaciones y
 * transportistas exigen la clave del dashboard, que agrega el proxy de Vite
 * del lado servidor (server/dashboardProxy.ts): el navegador nunca la conoce.
 */

/** Forma del error definida en components.schemas.ProblemDetails. */
interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  public readonly status: number;
  public readonly code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.status = status;
    this.code = code;
    this.name = "ApiError";
  }
}

const MENSAJES_POR_CODIGO: Record<string, string> = {
  REFERENCE_NOT_FOUND: "El teléfono, la patente o el código de finca no existen en los datos sembrados o no están asociados.",
  APPOINTMENT_NOT_FOUND: "El turno ya no existe.",
  ACTIVE_APPOINTMENT_EXISTS: "El camión ya tiene un turno activo.",
  NO_CAPACITY: "No hay capacidad disponible en las próximas ventanas.",
  INVALID_TRANSITION: "El turno cambió de estado y la acción ya no es válida. Se recargaron los datos.",
  UNAUTHENTICATED:
    "El servidor del frontend no tiene configurada la clave del dashboard (AGROFLOW_DASHBOARD_KEY) o la API la rechazó.",
  DUPLICATE_DNI: "Ya existe un transportista con ese DNI.",
  DUPLICATE_WHATSAPP: "Ya existe un transportista con ese WhatsApp.",
  DUPLICATE_IDENTIFIER: "El DNI o el WhatsApp ya están registrados.",
  RESOURCE_INACTIVE: "El transportista está inhabilitado.",
  DRIVER_NOT_FOUND: "El transportista ya no existe.",
  CONVERSATION_NOT_FOUND: "La conversación ya no existe.",
};

function mensajeDeError(status: number, body: ProblemDetails | null): string {
  if (body?.code && MENSAJES_POR_CODIGO[body.code]) return MENSAJES_POR_CODIGO[body.code];

  if (status === 400) {
    const campos = body?.errors
      ? Object.entries(body.errors).map(([campo, msgs]) => `${campo}: ${msgs.join(", ")}`)
      : [];
    const detalle = campos.length > 0 ? campos.join(" · ") : body?.detail ?? body?.title;
    return `Datos inválidos${detalle ? `: ${detalle}` : "."}`;
  }
  if (status === 404) return body?.title ?? "Recurso inexistente.";
  if (status === 409) return body?.title ?? "La operación entra en conflicto con el estado actual.";
  if (status >= 500) {
    return `Error interno de la API${body?.traceId ? ` (traceId ${body.traceId})` : ""}. Reintentá en unos segundos.`;
  }
  return body?.title ?? `Error ${status} al llamar a la API.`;
}

export class ApiClient {
  private readonly baseUrl: string;

  /**
   * @param baseUrl URL absoluta de la API, o "" para llamar al mismo origen
   * (el proxy de Vite reenvía /api a VITE_API_URL).
   */
  constructor(baseUrl: string) {
    this.baseUrl = baseUrl.replace(/\/+$/, "");
  }

  async get<T>(path: string, query?: Record<string, string | undefined>): Promise<T> {
    const url = new URL(`${this.baseUrl}${path}`, globalThis.location?.origin);
    if (query) {
      Object.entries(query).forEach(([k, v]) => {
        if (v !== undefined && v !== "") url.searchParams.set(k, v);
      });
    }
    return this.request<T>(url.toString(), { method: "GET" });
  }

  async post<T>(path: string, body?: unknown, headers?: Record<string, string>): Promise<T> {
    return this.request<T>(`${this.baseUrl}${path}`, {
      method: "POST",
      body: body === undefined ? undefined : JSON.stringify(body),
      headers,
    });
  }

  async put<T>(path: string, body: unknown, headers?: Record<string, string>): Promise<T> {
    return this.request<T>(`${this.baseUrl}${path}`, {
      method: "PUT",
      body: JSON.stringify(body),
      headers,
    });
  }

  async patch<T>(path: string, body?: unknown): Promise<T> {
    return this.request<T>(`${this.baseUrl}${path}`, {
      method: "PATCH",
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  }

  async delete(path: string): Promise<void> {
    await this.request<void>(`${this.baseUrl}${path}`, { method: "DELETE" });
  }

  private async request<T>(url: string, init: RequestInit): Promise<T> {
    let response: Response;

    try {
      response = await fetch(url, {
        ...init,
        headers: {
          Accept: "application/json, application/problem+json",
          ...(init.body !== undefined ? { "Content-Type": "application/json" } : {}),
          ...(init.headers ?? {}),
        },
      });
    } catch {
      // fetch solo rechaza por fallo de red, no por status 4xx/5xx.
      const destino = this.baseUrl || "el proxy de Vite (VITE_API_URL)";
      throw new ApiError(0, "NETWORK_ERROR", `No se pudo conectar con la API a través de ${destino}. ¿Está corriendo?`);
    }

    if (!response.ok) {
      let body: ProblemDetails | null = null;
      try {
        body = await response.json();
      } catch {
        // el cuerpo puede no ser JSON (ej. una página de error del host)
      }
      throw new ApiError(response.status, body?.code ?? "UNKNOWN_ERROR", mensajeDeError(response.status, body));
    }

    if (response.status === 204) return undefined as T;

    return (await response.json()) as T;
  }
}

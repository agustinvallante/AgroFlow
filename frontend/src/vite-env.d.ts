/// <reference types="vite/client" />

/**
 * Tipado de las variables de entorno. Sin esto, import.meta.env.VITE_API_URL
 * es `any` y TypeScript no avisa si escribís mal el nombre.
 */
interface ImportMetaEnv {
  readonly VITE_DATA_SOURCE?: "mock" | "http";
  readonly VITE_API_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

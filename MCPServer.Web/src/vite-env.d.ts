/// <reference types="vite/client" />

interface ImportMeta {
  readonly env: {
    readonly MODE: string;
    readonly BASE_URL: string;
    readonly PROD: boolean;
    readonly DEV: boolean;
    readonly VITE_API_URL?: string;
    readonly [key: string]: string | boolean | undefined;
  };
}

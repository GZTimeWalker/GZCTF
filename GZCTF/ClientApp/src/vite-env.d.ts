/// <reference types="vite/client" />
/// <reference types="vite-plugin-pages/client-react" />

interface ImportMetaEnv {
  readonly VITE_APP_TIMESTAMP: string
  readonly VITE_APP_GIT_SHA: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

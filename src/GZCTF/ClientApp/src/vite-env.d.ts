/// <reference types="vite/client" />
/// <reference types="vite-plugin-pages/client-react" />

interface ImportMetaEnv {
  readonly VITE_APP_BUILD_TIMESTAMP: string
  readonly VITE_APP_GIT_SHA: string
  readonly VITE_APP_GIT_NAME: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

declare module 'virtual:i18next-loader' {
  declare const resources: import('i18next').Resource

  export default resources
}

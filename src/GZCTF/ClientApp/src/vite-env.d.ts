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

declare module 'virtual:i18n-manifest' {
  declare const manifest: Record<string, string>

  export default manifest
}

declare module 'virtual:contributors' {
  interface Contributor {
    login: string
    html_url: string
    avatar_url: string
    contributions: number
  }

  const contributors: Contributor[]

  export default contributors
}

/// <reference types="vite/client" />
/// <reference types="vite-plugin-pages/client-react" />

declare module '@creepjs/*' {
  const content: any;
  export default content;
  export const getFingerprint: any;
  export const getLies: any;
  export const getTrash: any;
  export const caniuse: any;
  export const getCapturedErrors: any;
  export const hashify: any;
  export const IS_BLINK: any;
  export const LowerEntropy: any;
  export const braveBrowser: any;
  export const getBraveMode: any;
  export const getBraveUnprotectedParameters: any;
}

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

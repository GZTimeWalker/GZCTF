import type { Logger, Plugin } from 'vite'

export function headInfo(backendUrl: string): Plugin {
  let logger: Logger | undefined
  let warned = false

  return {
    name: 'gzctf-head-info',
    apply: 'serve',
    configResolved(config) {
      logger = config.logger
    },
    transformIndexHtml: {
      order: 'post',
      async handler(html) {
        try {
          const response = await fetch(new URL('/api/headinfo', backendUrl), {
            signal: AbortSignal.timeout(2000),
          })
          if (!response.ok || !response.headers.get('content-type')?.startsWith('text/plain')) {
            throw new Error('Head information is unavailable')
          }

          const comment = await response.text()
          warned = false
          return comment ? html.replace(/<\/head>/i, (closingTag) => `${comment}\n${closingTag}`) : html
        } catch {
          if (!warned) {
            logger?.warn('Could not fetch head information from the backend. The page will load without it.')
            warned = true
          }
          return html
        }
      },
    },
  }
}

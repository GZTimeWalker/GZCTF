import crypto from 'crypto'
import fs from 'fs'
import path from 'path'
import { createLogger } from 'vite'
import type { Plugin } from 'vite'

export default function i18nVirtualManifest(): Plugin {
  let manifest: Record<string, string> = {}
  let contents: Record<string, object> = {}
  const logger = createLogger()
  let localesDir: string
  let outputDir: string

  const reloadResources = () => {
    const newManifest: Record<string, string> = {}
    const newContents: Record<string, object> = {}

    try {
      fs.readdirSync(localesDir)
        .filter(function (file) {
          return fs.statSync(path.join(localesDir, file)).isDirectory()
        })
        .forEach((lang) => {
          const langDir = path.join(localesDir, lang)
          const files = fs.readdirSync(langDir).filter((file) => file.endsWith('.json'))

          const merged = files.reduce((acc, file) => {
            const key = file.replace('.json', '')
            const filePath = path.join(langDir, file)
            const content = JSON.parse(fs.readFileSync(filePath, 'utf-8'))
            return { ...acc, [key]: content }
          }, {})

          const contentString = JSON.stringify(merged)
          const hash = crypto.createHash('md5').update(contentString).digest('hex').slice(0, 8)
          const outputFileName = `${lang}.${hash}.json`

          newManifest[lang.toLowerCase()] = outputFileName
          newContents[outputFileName] = merged
        })

      manifest = newManifest
      contents = newContents
    } catch (e) {
      logger.error(`Error reading locales directory: ${e}`)
    }
  }

  return {
    name: 'vite-i18n-virtual-manifest',

    configResolved(config) {
      localesDir = path.resolve(config.root, path.join('src', 'locales'))
      outputDir = config.build.assetsDir
    },

    buildStart() {
      reloadResources()

      // emit files only in production mode
      if (process.env.NODE_ENV === 'production') {
        Object.keys(contents).forEach((file) => {
          this.emitFile({
            type: 'asset',
            fileName: path.join(outputDir, file),
            source: JSON.stringify(contents[file]),
          })
        })
      }

      this.addWatchFile(localesDir)
    },

    configureServer(server) {
      // handle requests for i18n resources like `/static/${lang}.${hash}.json`
      server.middlewares.use((req, res, next) => {
        if (!req.url?.startsWith('/static/')) {
          return next()
        }

        const file = req.url.slice('/static/'.length)
        const content = contents[file]

        if (!content) {
          return next()
        }

        res.setHeader('Content-Type', 'application/json')
        res.end(JSON.stringify(content))
      })

      // watch for changes in locales directory
      server.watcher.add(localesDir).on('change', (file) => {
        if (file.endsWith('.json') && file.includes('locales')) reloadResources()
      })
    },

    resolveId(id) {
      if (id === 'virtual:i18n-manifest') return id
      return null
    },

    load(id) {
      if (id !== 'virtual:i18n-manifest') return null
      return `export default ${JSON.stringify(manifest)}`
    },
  }
}

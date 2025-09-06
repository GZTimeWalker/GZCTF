import {
  transformerNotationDiff,
  transformerNotationHighlight,
  transformerNotationWordHighlight,
  transformerNotationFocus,
  transformerNotationErrorLevel,
} from '@shikijs/transformers'
import type { MarkedExtension, Token } from 'marked'
import { createHighlighterCore } from 'shiki/core'
import { createJavaScriptRegexEngine } from 'shiki/engine/javascript'

const highlighter = await createHighlighterCore({
  langs: [
    import('@shikijs/langs/astro'),
    import('@shikijs/langs/c'),
    import('@shikijs/langs/cpp'),
    import('@shikijs/langs/csharp'),
    import('@shikijs/langs/cs'),
    import('@shikijs/langs/css'),
    import('@shikijs/langs/dart'),
    import('@shikijs/langs/docker'),
    import('@shikijs/langs/dockerfile'),
    import('@shikijs/langs/graphql'),
    import('@shikijs/langs/gql'),
    import('@shikijs/langs/fsharp'),
    import('@shikijs/langs/gdscript'),
    import('@shikijs/langs/glsl'),
    import('@shikijs/langs/bash'),
    import('@shikijs/langs/go'),
    import('@shikijs/langs/html'),
    import('@shikijs/langs/javascript'),
    import('@shikijs/langs/json'),
    import('@shikijs/langs/latex'),
    import('@shikijs/langs/markdown'),
    import('@shikijs/langs/mdx'),
    import('@shikijs/langs/plsql'),
    import('@shikijs/langs/prisma'),
    import('@shikijs/langs/powershell'),
    import('@shikijs/langs/python'),
    import('@shikijs/langs/rust'),
    import('@shikijs/langs/sql'),
    import('@shikijs/langs/svelte'),
    import('@shikijs/langs/toml'),
    import('@shikijs/langs/yaml'),
    import('@shikijs/langs/vue'),
    import('@shikijs/langs/typescript'),
    import('@shikijs/langs/xml'),
  ],
  themes: [import('@shikijs/themes/material-theme-darker'), import('@shikijs/themes/material-theme-lighter')],
  engine: createJavaScriptRegexEngine(),
})

const highlight = (code: string, lang: string) => {
  return highlighter.codeToHtml(code, {
    lang,
    themes: {
      dark: 'material-theme-darker',
      light: 'material-theme-lighter',
    },
    cssVariablePrefix: '--code-',
    defaultColor: 'light-dark()',
    transformers: [
      transformerNotationDiff(),
      transformerNotationHighlight(),
      transformerNotationWordHighlight(),
      transformerNotationFocus(),
      transformerNotationErrorLevel(),
    ],
  })
}

export function ShikiExtension(): MarkedExtension {
  return {
    walkTokens: (token: Token) => {
      if (token.type !== 'code') return

      const { lang = 'text', text } = token

      Object.assign(token, {
        type: 'html',
        block: true,
        text: highlight(text, lang),
      })
    },
  }
}

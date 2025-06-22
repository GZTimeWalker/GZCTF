import {
  transformerNotationDiff,
  transformerNotationHighlight,
  transformerNotationWordHighlight,
  transformerNotationFocus,
  transformerNotationErrorLevel,
} from '@shikijs/transformers'
import type { MarkedExtension, Token } from 'marked'
import { createHighlighter } from 'shiki'
import { createJavaScriptRegexEngine } from 'shiki/engine/javascript'

const highlighter = await createHighlighter({
  langs: [
    'astro',
    'c',
    'cpp',
    'csharp',
    'c#',
    'cs',
    'css',
    'dart',
    'docker',
    'dockerfile',
    'graphql',
    'gql',
    'fsharp',
    'gdscript',
    'glsl',
    'bash',
    'go',
    'html',
    'javascript',
    'json',
    'latex',
    'markdown',
    'mdx',
    'plsql',
    'prisma',
    'powershell',
    'python',
    'rust',
    'sql',
    'svelte',
    'toml',
    'yaml',
    'vue',
    'typescript',
    'xml',
  ],
  themes: ['material-theme-darker', 'material-theme-lighter'],
  engine: createJavaScriptRegexEngine(),
})

const highlight = async (code: string, lang: string) => {
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
    async: true,
    walkTokens: async (token: Token) => {
      if (token.type !== 'code') return

      const { lang = 'text', text } = token

      Object.assign(token, {
        type: 'html',
        block: true,
        text: await highlight(text, lang),
      })
    },
  }
}

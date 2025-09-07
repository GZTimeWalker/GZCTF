import applescript from '@shikijs/langs/applescript'
import asm from '@shikijs/langs/asm'
import bash from '@shikijs/langs/bash'
import cLang from '@shikijs/langs/c'
import cpp from '@shikijs/langs/cpp'
import csharp from '@shikijs/langs/csharp'
import diff from '@shikijs/langs/diff'
import docker from '@shikijs/langs/docker'
import dockerfile from '@shikijs/langs/dockerfile'
import dotenv from '@shikijs/langs/dotenv'
import glsl from '@shikijs/langs/glsl'
import go from '@shikijs/langs/go'
import html from '@shikijs/langs/html'
import ini from '@shikijs/langs/ini'
import java from '@shikijs/langs/java'
import json from '@shikijs/langs/json'
import jsonc from '@shikijs/langs/jsonc'
import llvm from '@shikijs/langs/llvm'
import log from '@shikijs/langs/log'
import make from '@shikijs/langs/make'
import markdown from '@shikijs/langs/markdown'
import powershell from '@shikijs/langs/powershell'
import proto from '@shikijs/langs/proto'
import python from '@shikijs/langs/python'
import rust from '@shikijs/langs/rust'
import shellscript from '@shikijs/langs/shellscript'
import solidity from '@shikijs/langs/solidity'
import sql from '@shikijs/langs/sql'
import toml from '@shikijs/langs/toml'
import typescript from '@shikijs/langs/typescript'
import xml from '@shikijs/langs/xml'
import yaml from '@shikijs/langs/yaml'
import materialThemeDarker from '@shikijs/themes/material-theme-darker'
import materialThemeLighter from '@shikijs/themes/material-theme-lighter'
import {
  transformerNotationDiff,
  transformerNotationHighlight,
  transformerNotationWordHighlight,
  transformerNotationFocus,
  transformerNotationErrorLevel,
} from '@shikijs/transformers'
import type { MarkedExtension, Token } from 'marked'
import { createHighlighterCoreSync, HighlighterCore } from 'shiki/core'
import { createJavaScriptRegexEngine } from 'shiki/engine/javascript'
import css from '@shikijs/langs/css'

let highlighter: HighlighterCore | null = null

const initHighlighter = (): HighlighterCore => {
  if (!highlighter) {
    /* prettier-ignore */
    highlighter = createHighlighterCoreSync({
      langs: [
        cLang, cpp, csharp, css, docker, shellscript,
        dockerfile, glsl, bash, go, html, ini, java,
        jsonc, json, applescript, asm, diff, dotenv,
        llvm, log, make, proto, solidity, markdown,
        powershell, python, rust, sql, toml, yaml,
        typescript, xml,
      ],
      themes: [materialThemeDarker, materialThemeLighter],
      engine: createJavaScriptRegexEngine(),
    })
  }

  return highlighter
}

const highlight = (code: string, lang: string) => {
  if (lang === 'json') lang = 'jsonc'
  if (lang === 'js' || lang === 'javascript') lang = 'typescript'

  return initHighlighter().codeToHtml(code, {
    lang,
    themes: { dark: 'material-theme-darker', light: 'material-theme-lighter' },
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
      Object.assign(token, { type: 'html', block: true, text: highlight(text, lang) })
    },
  }
}

import astro from '@shikijs/langs/astro'
import bash from '@shikijs/langs/bash'
import cLang from '@shikijs/langs/c'
import cpp from '@shikijs/langs/cpp'
import cs from '@shikijs/langs/cs'
import csharp from '@shikijs/langs/csharp'
import dart from '@shikijs/langs/dart'
import docker from '@shikijs/langs/docker'
import dockerfile from '@shikijs/langs/dockerfile'
import fsharp from '@shikijs/langs/fsharp'
import gdscript from '@shikijs/langs/gdscript'
import glsl from '@shikijs/langs/glsl'
import go from '@shikijs/langs/go'
import gql from '@shikijs/langs/gql'
import graphql from '@shikijs/langs/graphql'
import html from '@shikijs/langs/html'
import javascript from '@shikijs/langs/javascript'
import json from '@shikijs/langs/json'
import latex from '@shikijs/langs/latex'
import markdown from '@shikijs/langs/markdown'
import mdx from '@shikijs/langs/mdx'
import plsql from '@shikijs/langs/plsql'
import powershell from '@shikijs/langs/powershell'
import prisma from '@shikijs/langs/prisma'
import python from '@shikijs/langs/python'
import rust from '@shikijs/langs/rust'
import sql from '@shikijs/langs/sql'
import svelte from '@shikijs/langs/svelte'
import toml from '@shikijs/langs/toml'
import typescript from '@shikijs/langs/typescript'
import vue from '@shikijs/langs/vue'
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
        astro, cLang, cpp, csharp, cs, css, dart, docker,
        dockerfile, graphql, gql, fsharp, gdscript, glsl, bash, go,
        html, javascript, json, latex, markdown, mdx, plsql, prisma,
        powershell, python, rust, sql, svelte, toml, yaml, vue,
        typescript, xml,
      ],
      themes: [materialThemeDarker, materialThemeLighter],
      engine: createJavaScriptRegexEngine(),
    })
  }

  return highlighter
}

const highlight = (code: string, lang: string) => {
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

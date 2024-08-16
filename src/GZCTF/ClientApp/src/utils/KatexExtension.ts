import katex from 'katex'
import {
  MarkedOptions,
  MarkedExtension,
  TokenizerAndRendererExtension,
  Tokens,
  RendererExtensionFunction,
} from 'marked'

const inlineRule = /^(\${1,2})(?!\$)((?:\\.|[^\\\n])*?(?:\\.|[^\\\n]))\1(?=[\s?!.,:？！。，：]|$)/
const blockRule = /^(\${1,2})\n((?:\\[^]|[^\\])+?)\n\1(?:\n|$)/

export function KatexExtension(options: MarkedOptions = {}): MarkedExtension {
  return {
    extensions: [
      inlineKatex(options, createRenderer(options, false)),
      blockKatex(options, createRenderer(options, true)),
    ],
  }
}

function createRenderer(options: MarkedOptions, newlineAfter: boolean): RendererExtensionFunction {
  return (token: Tokens.Generic) => {
    if (token.type === 'inlineKatex' || token.type === 'blockKatex') {
      return (
        katex.renderToString(token.text, { ...options, displayMode: token.displayMode }) +
        (newlineAfter ? '\n' : '')
      )
    }
  }
}

interface InlineKatex extends Tokens.Generic {
  type: 'inlineKatex'
  raw: string
  text: string
  displayMode: boolean
}

function inlineKatex(
  options: MarkedOptions,
  renderer: RendererExtensionFunction
): TokenizerAndRendererExtension {
  return {
    name: 'inlineKatex',
    level: 'inline',
    start(src: string) {
      let index
      let indexSrc = src

      while (indexSrc) {
        index = indexSrc.indexOf('$')
        if (index === -1) {
          return
        }

        if (index === 0 || indexSrc.charAt(index - 1) === ' ') {
          const possibleKatex = indexSrc.substring(index)

          if (possibleKatex.match(inlineRule)) {
            return index
          }
        }

        indexSrc = indexSrc.substring(index + 1).replace(/^\$+/, '')
      }
    },
    tokenizer(src: string): InlineKatex | undefined {
      const match = src.match(inlineRule)
      if (match) {
        return {
          type: 'inlineKatex',
          raw: match[0],
          text: match[2].trim(),
          displayMode: match[1].length === 2,
        }
      }
    },
    renderer,
  }
}

interface BlockKatex extends Tokens.Generic {
  type: 'blockKatex'
  raw: string
  text: string
  displayMode: boolean
}

function blockKatex(
  options: MarkedOptions,
  renderer: RendererExtensionFunction
): TokenizerAndRendererExtension {
  return {
    name: 'blockKatex',
    level: 'block',
    tokenizer(src): BlockKatex | undefined {
      const match = src.match(blockRule)
      if (match) {
        return {
          type: 'blockKatex',
          raw: match[0],
          text: match[2].trim(),
          displayMode: match[1].length === 2,
        }
      }
    },
    renderer,
  }
}

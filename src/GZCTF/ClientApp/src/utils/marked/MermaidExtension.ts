import type { MarkedExtension, Token } from 'marked'

const escapeHtml = (s: string) =>
  s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')

export function MermaidExtension(): MarkedExtension {
  return {
    walkTokens: (token: Token) => {
      if (token.type !== 'code') return

      const info = (token.lang ?? '').trim().toLowerCase()
      const lang = info.split(/\s+/)[0]
      const text = (token.text ?? '').trim()

      const looksLikeMermaid =
        /^(graph|flowchart|sequenceDiagram|classDiagram|stateDiagram|erDiagram|gantt|journey|mindmap|timeline)\b/i.test(text)

      if (lang !== 'mermaid' && !looksLikeMermaid) return

      const encoded = encodeURIComponent(text)
      Object.assign(token, {
        type: 'html',
        block: true,
        text: `<div class="mermaid" data-mermaid-src="${encoded}">${escapeHtml(text)}</div>`,
      })
    },
  }
}

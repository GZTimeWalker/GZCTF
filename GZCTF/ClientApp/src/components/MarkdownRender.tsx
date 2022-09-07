import katex from 'katex'
import 'katex/dist/katex.min.css'
import { marked } from 'marked'
import Prism from 'prismjs'
import { forwardRef } from 'react'
import { TypographyStylesProvider } from '@mantine/core'
import { useTypographyStyles } from '@Utils/useTypographyStyles'

interface MarkdownProps extends React.ComponentPropsWithoutRef<'div'> {
  source: string
}

const RenderReplacer = (func: any, replacer: (text: string) => string) => {
  const original = func
  return (...args: any[]) => {
    args[0] = replacer(args[0])
    return original(args)
  }
}

export const MarkdownRender = forwardRef<HTMLDivElement, MarkdownProps>((props, ref) => {
  const { classes, cx } = useTypographyStyles()
  const { source, ...others } = props

  const renderer = new marked.Renderer()

  const replacer = ((blockRegex, inlineRegex) => (text: string) => {
    text = text.replace(blockRegex, (match, expression) => {
      return katex.renderToString(expression, { displayMode: true, throwOnError: false })
    })

    text = text.replace(inlineRegex, (match, expression) => {
      return katex.renderToString(expression, { displayMode: false, throwOnError: false })
    })

    return text
  })(/\$\$([\s\S]+?)\$\$/g, /\$([^\n\s]+?)\$/g)

  renderer.paragraph = RenderReplacer(renderer.paragraph, replacer)
  renderer.text = RenderReplacer(renderer.text, replacer)

  Prism.manual = true

  marked.setOptions({
    highlight(code, lang) {
      if (Prism.languages[lang]) {
        return Prism.highlight(code, Prism.languages[lang], lang)
      } else {
        return code
      }
    },
    renderer,
  })

  return (
    <TypographyStylesProvider
      ref={ref}
      className={others.className ? cx(classes.root, others.className) : classes.root}
      {...others}
    >
      <div className="line-numbers" dangerouslySetInnerHTML={{ __html: marked.parse(source) }} />
    </TypographyStylesProvider>
  )
})

export default MarkdownRender

import katex from 'katex'
import 'katex/dist/katex.min.css'
import { marked } from 'marked'
import Prism from 'prismjs'
import { forwardRef } from 'react'
import { Sx, Text, TextProps, TypographyStylesProvider } from '@mantine/core'
import { useInlineStyles, useTypographyStyles } from '@Utils/useTypographyStyles'

export interface MarkdownProps extends React.ComponentPropsWithoutRef<'div'> {
  source: string
  sx?: Sx | (Sx | undefined)[]
}

interface InlineMarkdownProps extends TextProps {
  source: string
}

const RenderReplacer = (func: any, replacer: (text: string) => string) => {
  const original = func
  return (...args: any[]) => {
    args[0] = replacer(args[0])
    return original(args)
  }
}

export const InlineMarkdownRender = forwardRef<HTMLParagraphElement, InlineMarkdownProps>(
  (props, ref) => {
    const { source, ...others } = props
    const { classes, cx } = useInlineStyles()

    return (
      <Text
        ref={ref}
        className={others.className ? cx(classes.root, others.className) : classes.root}
        {...others}
        dangerouslySetInnerHTML={{
          __html: marked.parseInline(source, { silent: true }) ?? '',
        }}
      />
    )
  }
)

export const MarkdownRender = forwardRef<HTMLDivElement, MarkdownProps>((props, ref) => {
  const { classes, cx } = useTypographyStyles()
  const { source, ...others } = props

  const renderer = new marked.Renderer()

  const replacer = ((blockRegex, inlineRegex) => (text: string) => {
    text = text.replace(blockRegex, (_, expression) => {
      return katex.renderToString(expression, { displayMode: true, throwOnError: false })
    })

    text = text.replace(inlineRegex, (_, expression) => {
      return katex.renderToString(expression, { displayMode: false, throwOnError: false })
    })

    return text
  })(/\$\$([\s\S]+?)\$\$/g, /\$([\s\S]+?)\$/g)

  renderer.paragraph = RenderReplacer(renderer.paragraph, replacer)
  renderer.text = RenderReplacer(renderer.text, replacer)

  Prism.manual = true

  marked.setOptions({
    highlight(code, lang) {
      if (lang && Prism.languages[lang]) {
        return Prism.highlight(code, Prism.languages[lang], lang)
      } else {
        return code
      }
    },
    renderer,
    silent: true,
  })

  return (
    <TypographyStylesProvider
      ref={ref}
      className={others.className ? cx(classes.root, others.className) : classes.root}
      {...others}
    >
      <div dangerouslySetInnerHTML={{ __html: marked.parse(source) }} />
    </TypographyStylesProvider>
  )
})

export default MarkdownRender

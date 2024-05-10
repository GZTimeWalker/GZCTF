import { Text, TextProps, TypographyStylesProvider } from '@mantine/core'
import katex from 'katex'
import 'katex/dist/katex.min.css'
import { Marked } from 'marked'
import { markedHighlight } from 'marked-highlight'
import Prism from 'prismjs'
import { forwardRef } from 'react'
import classes from '@Styles/Typography.module.css'

export interface MarkdownProps extends React.ComponentPropsWithoutRef<'div'> {
  source: string
  withRightIcon?: boolean
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

const InlineRegex = /\$([\s\S]+?)\$/g
const BlockRegex = /\$\$([\s\S]+?)\$\$/g

export const InlineMarkdownRender = forwardRef<HTMLParagraphElement, InlineMarkdownProps>(
  (props, ref) => {
    const { source, ...others } = props
    const marked = new Marked()

    const renderer = new marked.Renderer()

    const replacer = (text: string) =>
      text.replace(InlineRegex, (_, expression) => {
        return katex.renderToString(expression, { displayMode: false, throwOnError: false })
      })

    renderer.text = RenderReplacer(renderer.text, replacer)

    marked.setOptions({
      renderer,
      silent: true,
    })

    return (
      <Text
        ref={ref}
        {...others}
        className={classes.inline}
        dangerouslySetInnerHTML={{
          __html: marked.parseInline(source) ?? '',
        }}
      />
    )
  }
)

export const MarkdownRender = forwardRef<HTMLDivElement, MarkdownProps>((props, ref) => {
  const { source, ...others } = props

  Prism.manual = true

  const marked = new Marked(
    markedHighlight({
      highlight(code, lang) {
        if (lang && Prism.languages[lang]) {
          return Prism.highlight(code, Prism.languages[lang], lang)
        } else {
          return code
        }
      },
    })
  )

  const renderer = new marked.Renderer()

  const replacer = (text: string) => {
    text = text.replace(BlockRegex, (_, expression) => {
      return katex.renderToString(expression, { displayMode: true, throwOnError: false })
    })

    text = text.replace(InlineRegex, (_, expression) => {
      return katex.renderToString(expression, { displayMode: false, throwOnError: false })
    })

    return text
  }

  renderer.paragraph = RenderReplacer(renderer.paragraph, replacer)
  renderer.text = RenderReplacer(renderer.text, replacer)

  marked.setOptions({
    renderer,
    silent: true,
  })

  return (
    <TypographyStylesProvider
      ref={ref}
      {...others}
      data-with-right-icon={props.withRightIcon || undefined}
      className={classes.root}
    >
      <div dangerouslySetInnerHTML={{ __html: marked.parse(source) }} />
    </TypographyStylesProvider>
  )
})

export default MarkdownRender

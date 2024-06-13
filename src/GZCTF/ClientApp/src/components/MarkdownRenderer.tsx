import { Text, TextProps, TypographyStylesProvider } from '@mantine/core'
import 'katex/dist/katex.min.css'
import { Marked } from 'marked'
import { markedHighlight } from 'marked-highlight'
import Prism from 'prismjs'
import { forwardRef } from 'react'
import { KatexExtension } from '@Utils/KatexExtension'
import classes from '@Styles/Typography.module.css'

export interface MarkdownProps extends React.ComponentPropsWithoutRef<'div'> {
  source: string
}

interface InlineMarkdownProps extends TextProps {
  source: string
}

export const InlineMarkdown = forwardRef<HTMLParagraphElement, InlineMarkdownProps>(
  (props, ref) => {
    const { source, ...others } = props
    const marked = new Marked()

    marked.use(KatexExtension()).setOptions({
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

export const Markdown = forwardRef<HTMLDivElement, MarkdownProps>((props, ref) => {
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

  marked.use(KatexExtension()).setOptions({
    silent: true,
  })

  return (
    <TypographyStylesProvider ref={ref} {...others} className={classes.root}>
      <div dangerouslySetInnerHTML={{ __html: marked.parse(source) }} />
    </TypographyStylesProvider>
  )
})

export default Markdown

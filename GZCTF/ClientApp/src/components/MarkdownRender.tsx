import { marked } from 'marked'
import Prism from 'prismjs'
import { forwardRef } from 'react'
import { TypographyStylesProvider } from '@mantine/core'
import { useTypographyStyles } from '@Utils/useTypographyStyles'

interface MarkdownProps extends React.ComponentPropsWithoutRef<'div'> {
  source: string
}

export const MarkdownRender = forwardRef<HTMLDivElement, MarkdownProps>((props, ref) => {
  const { classes, cx } = useTypographyStyles()
  const { source, ...others } = props

  marked.setOptions({
    highlight(code, lang) {
      if (Prism.languages[lang]) {
        return Prism.highlight(code, Prism.languages[lang], lang)
      } else {
        return code
      }
    },
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

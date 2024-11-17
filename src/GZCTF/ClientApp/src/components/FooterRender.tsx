import { TypographyStylesProvider } from '@mantine/core'
import { marked } from 'marked'
import { forwardRef } from 'react'
import { MarkdownProps } from '@Components/MarkdownRenderer'
import { useIsMobile } from '@Utils/ThemeOverride'
import classes from '@Styles/FooterRender.module.css'

export const FooterRender = forwardRef<HTMLDivElement, MarkdownProps>((props, ref) => {
  const { source, ...others } = props

  const isMobile = useIsMobile()

  // replace `<mbr/>` with `<br />` for mobile
  const mdSource = source.replace(/<mbr\/>/g, isMobile ? '<br />' : '')

  return (
    <TypographyStylesProvider ref={ref} c="dimmed" {...others} className={classes.root}>
      <div
        dangerouslySetInnerHTML={{
          __html: marked(mdSource, {
            silent: true,
          }),
        }}
      />
    </TypographyStylesProvider>
  )
})

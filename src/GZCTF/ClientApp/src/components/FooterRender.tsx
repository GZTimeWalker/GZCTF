import { TypographyStylesProvider } from '@mantine/core'
import { createStyles } from '@mantine/emotion'
import { marked } from 'marked'
import { forwardRef } from 'react'
import { MarkdownProps } from '@Components/MarkdownRender'
import { useIsMobile } from '@Utils/ThemeOverride'

const useFooterStyles = createStyles((theme, _, u) => {
  const cs = theme.colors
  const gray = cs.gray[6]

  return {
    root: {
      overflowX: 'auto',
      textAlign: 'center',
      fontSize: theme.fontSizes.sm,
      color: gray,

      '& p': {
        wordBreak: 'break-word',
        wordWrap: 'break-word',
        overflow: 'hidden',
        marginBottom: theme.spacing.xs,
      },

      '& :not(pre) > code': {
        color: gray,
        whiteSpace: 'normal',
        fontSize: '0.95em',
        backgroundColor: 'transparent',
        padding: `1px calc(${theme.spacing.xs} / 2)`,
        border: 'none',
      },

      '& strong': {
        [u.dark]: {
          color: cs.brand[6],
        },

        [u.light]: {
          color: cs.brand[7],
        },
      },

      '& a': {
        color: gray,
        textDecoration: 'none',
        transition: 'all 0.2s ease-in-out',
      },

      '& a:hover': {
        textDecoration: 'none',
      },
    },
  }
})

export const FooterRender = forwardRef<HTMLDivElement, MarkdownProps>((props, ref) => {
  const { classes, cx } = useFooterStyles()
  const { source, ...others } = props

  const isMobile = useIsMobile()

  // replace `<mbr/>` with `<br />` for mobile
  const mdSource = source.replace(/<mbr\/>/g, isMobile ? '<br />' : '')

  return (
    <TypographyStylesProvider
      ref={ref}
      className={others.className ? cx(classes.root, others.className) : classes.root}
      {...others}
    >
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

export default FooterRender

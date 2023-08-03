import { marked } from 'marked'
import { forwardRef } from 'react'
import { Sx, TypographyStylesProvider, createStyles } from '@mantine/core'
import { useIsMobile } from '@Utils/ThemeOverride'

interface MarkdownProps extends React.ComponentPropsWithoutRef<'div'> {
  source: string
  sx?: Sx | (Sx | undefined)[]
}

const useFooterStyles = createStyles((theme) => {
  const sc = (dark: any, light: any) => (theme.colorScheme === 'dark' ? dark : light)
  const cs = theme.colors

  return {
    root: {
      overflowX: 'auto',
      textAlign: 'center',
      fontSize: theme.fontSizes.sm,
      color: theme.fn.dimmed(),

      '& p': {
        wordBreak: 'break-word',
        wordWrap: 'break-word',
        overflow: 'hidden',
        marginBottom: theme.spacing.xs,
      },

      '& :not(pre) > code': {
        color: theme.fn.dimmed(),
        whiteSpace: 'normal',
        fontSize: '0.95em',
        backgroundColor: 'transparent',
        padding: `1px calc(${theme.spacing.xs} / 2)`,
        border: 'none',
      },

      '& strong': {
        color: cs.brand[sc(6, 7)],
      },

      '& a': {
        color: theme.fn.dimmed(),
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

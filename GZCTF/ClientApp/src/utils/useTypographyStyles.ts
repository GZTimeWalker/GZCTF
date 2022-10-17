import { createStyles } from '@mantine/core'

export const useTypographyStyles = createStyles((theme) => {
  const sc = (dark: any, light: any) => (theme.colorScheme === 'dark' ? dark : light)
  const cs = theme.colors

  return {
    root: {
      overflowX: 'auto',

      '& p': {
        wordBreak: 'break-word',
        wordWrap: 'break-word',
        overflow: 'hidden',
        marginBottom: theme.spacing.md,
      },

      '& ul, & ol': {
        paddingLeft: theme.fontSizes.lg * 1.5,

        [theme.fn.smallerThan('xs')]: {
          paddingLeft: theme.fontSizes.xs * 1.5,
        },
      },

      '& blockquote': {
        borderLeft: `4px solid ${sc(cs.dark[0], cs.gray[5])}`,
        padding: `${theme.spacing.md / 2}px ${theme.spacing.md}px`,
        color: theme.fn.rgba(sc(cs.dark[0], cs.gray[7]), 0.9),
        backgroundColor: theme.fn.rgba(theme.black, sc(0.1, 0.05)),
        marginBottom: theme.spacing.md,
        fontSize: '1em',

        '& p:last-child': {
          marginBottom: 0,
        },

        '& pre': {
          backgroundColor: theme.fn.rgba(sc(cs.dark[6], cs.white[1]), 0.8),
        },
      },

      '& :not(pre) > code': {
        whiteSpace: 'normal',
        fontSize: '0.95em',
        backgroundColor: theme.fn.rgba(theme.black, sc(0.1, 0.05)),
        padding: `1px ${theme.spacing.xs / 2}px`,
        border: 'none',
      },

      '& pre': {
        overflow: 'auto',
        position: 'relative',
        margin: '0.5em 0',
        padding: '1.25em 1em',
      },

      '& pre, & code': {
        fontFamily: theme.fontFamilyMonospace,
        fontWeight: 500,
        textAlign: 'left',
        whiteSpace: 'pre-wrap',
        wordWrap: 'break-word',
        wordBreak: 'normal',
        fontSize: '0.97em',
        lineHeight: 1.5,
        tabSize: 4,
        hyphens: 'none',

        color: sc(cs.white[2], cs.gray[7]),
        backgroundColor: sc(cs.dark[6], cs.white[1]),

        '& .namespace': {
          opacity: 0.8,
        },

        '& .token.id, & .token.important, & .token.keyword': {
          fontWeight: 'bold',
        },

        '& .token.atrule': {
          color: sc('#c792ea', '#7c4dff'),
        },

        '& .token.attr-name': {
          color: sc('#ffcb6b', '#39adb5'),
        },

        '& .token.attr-value': {
          color: sc('#a5e844', '#f6a434'),
        },

        '& .token.attribute': {
          color: sc('#a5e844', '#f6a434'),
        },

        '& .token.boolean': {
          color: sc('#c792ea', '#7c4dff'),
        },

        '& .token.builtin': {
          color: sc('#ffcb6b', '#39adb5'),
        },

        '& .token.cdata': {
          color: sc('#80cbc4', '#39adb5'),
        },

        '& .token.char': {
          color: sc('#80cbc4', '#39adb5'),
        },

        '& .token.class': {
          color: sc('#ffcb6b', '#39adb5'),
        },

        '& .token.class-name': {
          color: sc('#f2ff00', '#6182b8'),
        },

        '& .token.comment': {
          color: sc('#616161', '#aabfc9'),
        },

        '& .token.constant': {
          color: sc('#c792ea', '#7c4dff'),
        },

        '& .token.deleted': {
          color: sc('#ff6666', '#e53935'),
        },

        '& .token.doctype': {
          color: sc('#616161', '#aabfc9'),
        },

        '& .token.entity': {
          color: sc('#ff6666', '#e53935'),
        },

        '& .token.function': {
          color: sc('#c792ea', '#7c4dff'),
        },

        '& .token.hexcode': {
          color: sc('#f2ff00', '#f76d47'),
        },

        '& .token.id': {
          color: sc('#c792ea', '#7c4dff'),
        },

        '& .token.important': {
          color: sc('#c792ea', '#7c4dff'),
        },

        '& .token.inserted': {
          color: sc('#80cbc4', '#39adb5'),
        },

        '& .token.keyword': {
          color: sc('#c792ea', '#7c4dff'),
        },

        '& .token.number': {
          color: sc('#fd9170', '#f76d47'),
        },

        '& .token.operator': {
          color: sc('#89ddff', '#39adb5'),
        },

        '& .token.prolog': {
          color: sc('#616161', '#aabfc9'),
        },

        '& .token.property': {
          color: sc('#80cbc4', '#39adb5'),
        },

        '& .token.pseudo-class': {
          color: sc('#a5e844', '#f6a434'),
        },

        '& .token.pseudo-element': {
          color: sc('#a5e844', '#f6a434'),
        },

        '& .token.punctuation': {
          color: sc('#89ddff', '#39adb5'),
        },

        '& .token.regex': {
          color: sc('#f2ff00', '#6182b8'),
        },

        '& .token.selector': {
          color: sc('#ff6666', '#e53935'),
        },

        '& .token.string': {
          color: sc('#a5e844', '#84a657'),
        },

        '& .token.symbol': {
          color: sc('#c792ea', '#7c4dff'),
        },

        '& .token.tag': {
          color: sc('#ff6666', '#e53935'),
        },

        '& .token.unit': {
          color: sc('#fd9170', '#f76d47'),
        },

        '& .token.url': {
          color: sc('#ff6666', '#e53935'),
        },

        '& .token.variable': {
          color: sc('#ff6666', '#e53935'),
        },
      },
    },
  }
})

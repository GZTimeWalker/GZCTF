import { alpha } from '@mantine/core'
import { createStyles } from '@mantine/emotion'

export const useTypographyStyles = createStyles((theme, _, u) => {
  const sc = (propName: string, dark: string, light: string) => {
    return {
      [u.dark]: {
        [propName]: dark,
      },

      [u.light]: {
        [propName]: light,
      },
    }
  }

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
        paddingLeft: `calc(${theme.fontSizes.lg} * 1.5)`,

        [u.smallerThan('xs')]: {
          paddingLeft: `calc(${theme.fontSizes.xs} * 1.5)`,
        },
      },

      '& strong': {
        ...sc('color', cs.brand[6], cs.brand[7]),
      },

      '& blockquote': {
        padding: `calc(${theme.spacing.xs} / 2) ${theme.spacing.md}`,

        ...sc('color', alpha(cs.dark[0], 0.9), alpha(cs.gray[7], 0.9)),
        ...sc('backgroundColor', alpha(theme.black, 0.1), alpha(theme.white, 0.05)),

        ...sc('borderLeftColor', `4px solid ${cs.dark[0]}`, `4px solid ${cs.gray[5]}`),

        marginBottom: theme.spacing.md,
        fontSize: '1em',

        '& p:last-child': {
          marginBottom: 0,
        },

        '& pre': {
          // backgroundColor: alpha(sc(cs.dark[6], cs.light[1]), 0.8),
          ...sc('backgroundColor', alpha(cs.dark[6], 0.8), alpha(cs.light[1], 0.8)),
        },
      },

      '& :not(pre) > code': {
        whiteSpace: 'normal',
        fontSize: '0.95em',
        padding: `1px calc(${theme.spacing.xs} / 2)`,
        border: 'none',
        // backgroundColor: alpha(theme.black, sc(0.1, 0.05)),

        ...sc('backgroundColor', alpha(theme.black, 0.1), alpha(theme.white, 0.05)),
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

        // backgroundColor: sc(cs.dark[6], cs.light[1]),

        ...sc('color', cs.light[2], cs.gray[7]),
        ...sc('backgroundColor', cs.dark[6], cs.light[1]),

        '& .namespace': {
          opacity: 0.8,
        },

        '& .token.id, & .token.important, & .token.keyword': {
          fontWeight: 'bold',
        },

        '& .token.atrule': {
          ...sc('color', '#c792ea', '#7c4dff'),
        },

        '& .token.attr-name': {
          ...sc('color', '#ffcb6b', '#39adb5'),
        },

        '& .token.attr-value': {
          ...sc('color', '#a5e844', '#f6a434'),
        },

        '& .token.attribute': {
          ...sc('color', '#a5e844', '#f6a434'),
        },

        '& .token.boolean': {
          ...sc('color', '#c792ea', '#7c4dff'),
        },

        '& .token.builtin': {
          ...sc('color', '#ffcb6b', '#39adb5'),
        },

        '& .token.cdata': {
          ...sc('color', '#80cbc4', '#39adb5'),
        },

        '& .token.char': {
          ...sc('color', '#80cbc4', '#39adb5'),
        },

        '& .token.class': {
          ...sc('color', '#ffcb6b', '#39adb5'),
        },

        '& .token.class-name': {
          ...sc('color', '#f2ff00', '#6182b8'),
        },

        '& .token.comment': {
          ...sc('color', '#616161', '#aabfc9'),
        },

        '& .token.constant': {
          ...sc('color', '#c792ea', '#7c4dff'),
        },

        '& .token.deleted': {
          ...sc('color', '#ff6666', '#e53935'),
        },

        '& .token.doctype': {
          ...sc('color', '#616161', '#aabfc9'),
        },

        '& .token.entity': {
          ...sc('color', '#ff6666', '#e53935'),
        },

        '& .token.function': {
          ...sc('color', '#c792ea', '#7c4dff'),
        },

        '& .token.hexcode': {
          ...sc('color', '#f2ff00', '#f76d47'),
        },

        '& .token.id': {
          ...sc('color', '#c792ea', '#7c4dff'),
        },

        '& .token.important': {
          ...sc('color', '#c792ea', '#7c4dff'),
        },

        '& .token.inserted': {
          ...sc('color', '#80cbc4', '#39adb5'),
        },

        '& .token.keyword': {
          ...sc('color', '#c792ea', '#7c4dff'),
        },

        '& .token.number': {
          ...sc('color', '#fd9170', '#f76d47'),
        },

        '& .token.operator': {
          ...sc('color', '#89ddff', '#39adb5'),
        },

        '& .token.prolog': {
          ...sc('color', '#616161', '#aabfc9'),
        },

        '& .token.property': {
          ...sc('color', '#80cbc4', '#39adb5'),
        },

        '& .token.pseudo-class': {
          ...sc('color', '#a5e844', '#f6a434'),
        },

        '& .token.pseudo-element': {
          ...sc('color', '#a5e844', '#f6a434'),
        },

        '& .token.punctuation': {
          ...sc('color', '#89ddff', '#39adb5'),
        },

        '& .token.regex': {
          ...sc('color', '#f2ff00', '#6182b8'),
        },

        '& .token.string': {
          ...sc('color', '#a5e844', '#84a657'),
        },

        '& .token.symbol': {
          ...sc('color', '#c792ea', '#7c4dff'),
        },

        '& .token.unit': {
          ...sc('color', '#fd9170', '#f76d47'),
        },

        '& .token.selector, & .token.tag, & .token.url, & .token.variable': {
          ...sc('color', '#ff6666', '#e53935'),
        },
      },
    },
  }
})

export const useInlineStyles = createStyles((theme, _, u) => {
  const sc = (propName: string, dark: string, light: string) => {
    return {
      [u.dark]: {
        [propName]: dark,
      },

      [u.light]: {
        [propName]: light,
      },
    }
  }
  const cs = theme.colors

  return {
    root: {
      wordBreak: 'break-word',
      wordWrap: 'break-word',

      '& code': {
        whiteSpace: 'normal',
        fontWeight: 500,
        backgroundColor: 'transparent',
        fontFamily: theme.fontFamilyMonospace,
        padding: `0 2px`,
        border: 'none',
      },

      '& strong': {
        ...sc('color', cs.brand[6], cs.brand[7]),
      },

      '& a': {
        ...sc('color', cs.brand[6], cs.brand[7]),
        textDecoration: 'underline',
        transition: 'all 0.2s ease-in-out',
      },

      '& a:hover': {
        textDecoration: 'none',
      },
    },
  }
})

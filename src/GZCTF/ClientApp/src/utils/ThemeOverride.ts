import {
  ActionIcon,
  Badge,
  Loader,
  MantineThemeOverride,
  Modal,
  Popover,
  Switch,
  Tabs,
  createTheme,
  darken,
  rem,
  useMantineTheme,
} from '@mantine/core'
import { createStyles, keyframes } from '@mantine/emotion'
import { useMediaQuery } from '@mantine/hooks'

export const CustomTheme: MantineThemeOverride = createTheme({
  colors: {
    gray: [
      '#EBEBEB',
      '#CFCFCF',
      '#B3B3B3',
      '#969696',
      '#7A7A7A',
      '#5E5E5E',
      '#414141',
      '#252525',
      '#202020',
      '#141414',
    ],
    brand: [
      '#A7FFEB',
      '#64FFDA',
      '#25EEBA',
      '#1DE9B6',
      '#0AD7AF',
      '#04CAAB',
      '#02BFA5',
      '#009985',
      '#007F6E',
      '#005A4C',
    ],
    alert: [
      '#FFB4B4',
      '#FFA0A0',
      '#FF8c8c',
      '#FF7878',
      '#FF6464',
      '#FE5050',
      '#FE3c3c',
      '#FE2828',
      '#FC1414',
      '#FC0000',
    ],
    light: [
      '#FFFFFF',
      '#F8F8F8',
      '#EFEFEF',
      '#E0E0E0',
      '#DFDFDF',
      '#D0D0D0',
      '#CFCFCF',
      '#C0C0C0',
      '#BFBFBF',
      '#B0B0B0',
    ],
    dark: [
      '#d5d7d7',
      '#acaeae',
      '#8c8f8f',
      '#666969',
      '#4d4f4f',
      '#343535',
      '#2b2c2c',
      '#1d1e1e',
      '#0c0d0d',
      '#010101',
    ],
  },
  primaryColor: 'brand',
  fontFamily:
    "'IBM Plex Sans', -apple-system, BlinkMacSystemFont, Helvetica Neue, PingFang SC, Microsoft YaHei, Source Han Sans SC, Noto Sans CJK SC, sans-serif",
  fontFamilyMonospace:
    "'JetBrains Mono', ui-monospace, SFMono-Regular, Monaco, Consolas, 'Courier New', monospace, 'IBM Plex Sans', sans-serif",
  headings: {
    fontFamily: "'IBM Plex Sans', sans-serif",
  },
  breakpoints: {
    xs: '30em',
    sm: '48em',
    md: '64em',
    lg: '74em',
    xl: '90em',
    w18: '1800px',
    w24: '2400px',
    w30: '3000px',
    w36: '3600px',
    w42: '4200px',
    w48: '4800px',
  },
  components: {
    Loader: Loader.extend({
      defaultProps: {
        type: 'bars',
      },
    }),
    Switch: Switch.extend({
      styles: {
        body: {
          alignItems: 'center',
        },
        labelWrapper: {
          display: 'flex',
        },
      },
    }),
    Modal: Modal.extend({
      defaultProps: {
        centered: true,
        styles: {
          title: {
            fontWeight: 'bold',
          },
        },
      },
    }),
    Popover: Popover.extend({
      defaultProps: {
        withinPortal: true,
      },
    }),
    ActionIcon: ActionIcon.extend({
      defaultProps: {
        variant: 'transparent',
      },
    }),
    Badge: Badge.extend({
      defaultProps: {
        variant: 'outline',
      },
    }),
    Tabs: Tabs.extend({
      styles: {
        tab: {
          padding: '0.5rem',
          fontWeight: 500,
        },
      },
    }),
  },
})

export const useTableStyles = createStyles((theme, _, u) => ({
  mono: {
    fontFamily: theme.fontFamilyMonospace,
  },
  fade: {
    animation: `${keyframes`0% {
                              opacity: 0;
                            }
                              100% {
                                opacity: 1;
                              }`} 0.5s linear`,
  },
  table: {
    '& thead tr th': {
      position: 'sticky',
      top: 0,
      zIndex: 195,

      [u.dark]: {
        backgroundColor: theme.colors.dark[7],
      },

      [u.light]: {
        backgroundColor: theme.white,
      },
    },

    '& tbody tr td': {
      whiteSpace: 'nowrap',
    },
  },
}))

export const useTooltipStyles = createStyles((theme, _, u) => ({
  tooltip: {
    fontWeight: 500,
    boxShadow: theme.shadows.md,

    [u.dark]: {
      color: theme.colors.gray[0],
      backgroundColor: darken(theme.colors.gray[6], 0.4),
    },

    [u.light]: {
      color: theme.colors.gray[7],
      backgroundColor: theme.colors.light[0],
    },
  },
  arrow: {
    boxShadow: theme.shadows.md,
  },
}))

export const useIsMobile = (limit?: number) => {
  const theme = useMantineTheme()
  const isMobile = useMediaQuery(`(max-width: ${limit ? `${limit}px` : theme.breakpoints.sm})`)
  return isMobile
}

interface FixedButtonProps {
  right?: string
  bottom?: string
}

export const useFixedButtonStyles = createStyles((_, { right, bottom }: FixedButtonProps) => ({
  fixedButton: {
    position: 'fixed',
    bottom,
    right,
    boxShadow:
      '0 1px 3px rgb(0 0 0 / 5%), rgb(0 0 0 / 5%) 0px 28px 23px -7px, rgb(0 0 0 / 4%) 0px 12px 12px -7px',
    zIndex: 1000,

    '@media print': {
      display: 'none',
    },
  },
}))

export const useIconStyles = createStyles((theme, _, u) => ({
  triangle: {
    [u.dark]: {
      fill: theme.white,
    },

    [u.light]: {
      fill: theme.colors.gray[6],
    },
  },
  front: {
    fill: theme.colors[theme.primaryColor][4],
  },
  mid: {
    fill: theme.colors[theme.primaryColor][6],
  },
  back: {
    fill: theme.colors[theme.primaryColor][8],
  },
}))

export const useBannerStyles = createStyles((theme, _, u) => ({
  root: {
    position: 'relative',
    display: 'flex',
    justifyContent: 'center',
    backgroundSize: 'cover',
    backgroundPosition: 'center center',
    padding: `calc(${theme.spacing.xl} * 3) 0`,

    [u.dark]: {
      background: 'rgba(0,0,0,0.2)',
    },

    [u.light]: {
      background: theme.white,
    },

    [u.smallerThan('sm')]: {
      justifyContent: 'start',
    },

    [u.smallerThan('md')]: {
      padding: `calc(${theme.spacing.xl} * 1.5) 1rem`,
    },
  },
  container: {
    position: 'relative',
    maxWidth: '960px',
    width: '100%',
    zIndex: 1,

    [u.smallerThan('md')]: {
      padding: `${theme.spacing.md} calc(${theme.spacing.md} * 2)`,
    },
  },
  flexGrowAtSm: {
    flexGrow: 0,

    [u.smallerThan('sm')]: {
      flexGrow: 1,
    },
  },
  description: {
    color: theme.white,
    maxWidth: 600,
  },
  title: {
    fontSize: `calc(${theme.fontSizes.xl} * 2.2)`,
    fontWeight: 900,
    lineHeight: 1.1,

    [u.dark]: {
      color: theme.colors.light[0],
    },

    [u.light]: {
      color: theme.colors.gray[6],
    },

    [u.smallerThan('md')]: {
      maxWidth: '100%',
      fontSize: `calc(${theme.fontSizes.xl} * 1.8)`,
      lineHeight: 1.15,
    },
  },
  content: {
    paddingTop: '1rem',
  },
  banner: {
    maxWidth: '50%',
    height: '100%',
    width: '40vw',

    [u.smallerThan('sm')]: {
      display: 'none',
    },
  },
  date: {
    [u.dark]: {
      color: theme.colors.light[0],
    },

    [u.light]: {
      color: theme.colors.gray[6],
    },
  },
}))

export const useUploadStyles = createStyles(() => ({
  uploadButton: {
    position: 'relative',
    transition: 'background-color 150ms ease',
  },

  uploadProgress: {
    position: 'absolute',
    bottom: -1,
    right: -1,
    left: -1,
    top: -1,
    height: 'auto',
    backgroundColor: 'transparent',
    zIndex: 0,
  },

  uploadLabel: {
    position: 'relative',
    zIndex: 1,
  },

  hoverButton: {
    '&:hover': {
      cursor: 'pointer',
    },
  },
}))

interface UseDisplayInputStylesProps {
  ff?: 'monospace' | 'text'
  fw?: React.CSSProperties['fontWeight']
  lh?: React.CSSProperties['lineHeight']
}

export const useDisplayInputStyles = createStyles(
  (theme, { fw = 'normal', lh = '1.5rem', ff = 'text' }: UseDisplayInputStylesProps) => ({
    wrapper: {
      width: '100%',
    },
    input: {
      fontWeight: fw,
      fontFamily: ff === 'text' ? theme.fontFamily : theme.fontFamilyMonospace,
      height: lh,
      lineHeight: lh,
      cursor: 'auto',
      userSelect: 'none',
      minHeight: '1rem',
      maxHeight: '2rem',
    },
  })
)

export const useAccordionStyles = createStyles((theme, _, u) => ({
  root: {
    borderRadius: theme.radius.sm,

    [u.dark]: {
      backgroundColor: theme.colors.dark[6],
    },

    [u.light]: {
      backgroundColor: theme.colors.gray[0],
    },
  },

  item: {
    border: '1px solid rgba(0,0,0,0.2)',
    position: 'relative',

    [u.dark]: {
      backgroundColor: theme.colors.dark[6],
    },

    [u.light]: {
      backgroundColor: theme.colors.gray[0],
    },

    '&[data-active]': {
      boxShadow: theme.shadows.md,

      [u.dark]: {
        backgroundColor: theme.colors.dark[7],
      },

      [u.light]: {
        backgroundColor: theme.white,
      },
    },
  },

  label: {
    padding: '0',
  },

  control: {
    padding: '8px 4px',

    '&:hover': {
      cursor: 'pointer',
      background: 'transparent',
    },
  },
}))

export const useHoverCardStyles = createStyles((theme, _, u) => ({
  root: {
    cursor: 'pointer',
    transition: 'filter .2s',

    '&:hover': {
      [u.dark]: {
        filter: 'brightness(1.2)',
      },

      [u.light]: {
        filter: 'brightness(.97)',
      },
    },
  },
}))

const FOOTER_HEIGHT = rem(240)

export const useFooterStyles = createStyles((theme, _, u) => ({
  spacer: {
    height: FOOTER_HEIGHT,
  },

  wrapper: {
    position: 'fixed',
    zIndex: 5,
    bottom: 0,
    left: 0,
    right: 0,
    height: FOOTER_HEIGHT,
    paddingLeft: `calc(var(--app-shell-navbar-offset, 0rem) + var(--mantine-spacing-xl) * 2)`,
    paddingRight: `calc(var(--mantine-spacing-xl) * 2)`,

    [u.smallerThan('md')]: {
      padding: `var(--mantine-spacing-xl)`,
    },

    [u.dark]: {
      backgroundColor: theme.colors.dark[7],
    },

    [u.light]: {
      backgroundColor: theme.colors.light[2],
    },
  },
}))

export const useLogoStyles = createStyles((theme, _, u) => ({
  title: {
    [u.dark]: {
      color: theme.colors.light[0],
    },

    [u.light]: {
      color: theme.colors.gray[6],
    },
  },
  brand: {
    color: theme.colors[theme.primaryColor][4],
  },
  bio: {
    fontFamily: theme.fontFamilyMonospace,
    fontWeight: 'bold',
    fontSize: '1.5rem',

    [u.dark]: {
      color: theme.colors.gray[2],
    },

    [u.light]: {
      color: theme.colors.dark[4],
    },
  },
  blink: {
    fontFamily: theme.fontFamilyMonospace,
    fontWeight: 'bold',
    fontSize: '1.5rem',
    animation: `${keyframes`0%, 100% {
                              opacity: 0;
                            }
                              50% {
                                opacity: 1;
                              }`} 1s infinite steps(1,start)`,
  },
  watermark: {
    position: 'absolute',
    fontSize: '12rem',
    fontWeight: 'bold',
    opacity: 0.05,
    transform: 'scale(1.5)',
    userSelect: 'none',
  },
}))

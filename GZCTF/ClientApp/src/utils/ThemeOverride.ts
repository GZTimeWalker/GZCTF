import { createStyles, keyframes, MantineThemeOverride, useMantineTheme } from '@mantine/core'
import { MIME_TYPES } from '@mantine/dropzone'
import { useViewportSize } from '@mantine/hooks'

export const ThemeOverride: MantineThemeOverride = {
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
    ],
    alert: [
      '#FF9090',
      '#FF8080',
      '#FF7070',
      '#FF6060',
      '#FF5050',
      '#FE4040',
      '#FE3030',
      '#FE2020',
      '#FC1010',
      '#FC0000',
    ],
    white: [
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
  fontFamily: "'IBM Plex Sans', -apple-system, BlinkMacSystemFont, Helvetica Neue, PingFang SC, Microsoft YaHei, Source Han Sans SC, Noto Sans CJK SC, sans-serif",
  fontFamilyMonospace: "'JetBrains Mono', monospace",
  headings: {
    fontFamily: "'IBM Plex Sans', sans-serif",
  },
  loader: 'bars',
  components: {
    Switch: {
      styles: {
        body: {
          alignItems: 'center',
        },
        labelWrapper: {
          display: 'flex',
        }
      },
    },
  },
}

export const useTableStyles = createStyles((theme) => ({
  mono: {
    fontFamily: theme.fontFamilyMonospace,
  },
  fade: {
    animation: `${keyframes`0% {opacity:0;} 100% {opacity:1;}`} 0.5s linear`,
  },
  table: {
    '& thead tr th': {
      position: 'sticky',
      top: 0,
      zIndex: 195,
      backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[7] : theme.white,
    },

    '& tbody tr td': {
      whiteSpace: 'nowrap',
    },
  },
}))

export const useTooltipStyles = createStyles((theme) => ({
  tooltip: {
    fontWeight: 500,
    background:
      theme.colorScheme === 'dark'
        ? theme.fn.darken(theme.colors.gray[6], 0.4)
        : theme.colors.white[0],
    boxShadow: theme.shadows.md,
    color: theme.colorScheme === 'dark' ? theme.colors.white[2] : theme.colors.gray[7],
  },
  arrow: {
    boxShadow: theme.shadows.md,
  },
}))

export const useIsMobile = (limit?: number) => {
  const view = useViewportSize()
  const theme = useMantineTheme()
  return {
    loaded: view.width > 0,
    isMobile: view.width > 0 && view.width < (limit ?? theme.breakpoints.xs),
  }
}

export const ACCEPT_IMAGE_MIME_TYPE = [
  MIME_TYPES.png,
  MIME_TYPES.webp,
  MIME_TYPES.jpeg,
  MIME_TYPES.gif,
]

interface FixedButtonProps {
  right?: string
  bottom?: string
}

export const useFixedButtonStyles = createStyles((theme, { right, bottom }: FixedButtonProps) => ({
  fixedButton: {
    position: 'fixed',
    bottom: bottom,
    right: right,
    boxShadow:
      '0 1px 3px rgb(0 0 0 / 5%), rgb(0 0 0 / 5%) 0px 28px 23px -7px, rgb(0 0 0 / 4%) 0px 12px 12px -7px',
    zIndex: 1000,
  },
}))

export const useBannerStyles = createStyles((theme) => ({
  root: {
    position: 'relative',
    display: 'flex',
    background: theme.colorScheme === 'dark' ? ` rgba(0,0,0,0.2)` : theme.white,
    justifyContent: 'center',
    backgroundSize: 'cover',
    backgroundPosition: 'center center',
    padding: `${theme.spacing.xl * 3}px 0`,

    [theme.fn.smallerThan('sm')]: {
      justifyContent: 'start',
    },
  },
  container: {
    position: 'relative',
    maxWidth: '960px',
    width: '100%',
    zIndex: 1,

    [theme.fn.smallerThan('md')]: {
      padding: `${theme.spacing.md}px ${theme.spacing.md * 2}px`,
    },
  },
  flexGrowAtSm: {
    flexGrow: 0,

    [theme.fn.smallerThan('sm')]: {
      flexGrow: 1,
    },
  },
  description: {
    color: theme.white,
    maxWidth: 600,
  },
  title: {
    color: theme.colorScheme === 'dark' ? theme.colors.white[0] : theme.colors.gray[6],
    fontSize: 50,
    fontWeight: 900,
    lineHeight: 1.1,

    [theme.fn.smallerThan('md')]: {
      maxWidth: '100%',
      fontSize: 34,
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

    [theme.fn.smallerThan('sm')]: {
      display: 'none',
    },
  },
  date: {
    color: theme.colorScheme === 'dark' ? theme.colors.white[0] : theme.colors.gray[6],
  },
}))

import { createStyles, keyframes, MantineThemeOverride, useMantineTheme, rem } from '@mantine/core'
import { useMediaQuery } from '@mantine/hooks'

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
  fontFamily:
    "'IBM Plex Sans', -apple-system, BlinkMacSystemFont, Helvetica Neue, PingFang SC, Microsoft YaHei, Source Han Sans SC, Noto Sans CJK SC, sans-serif",
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
        },
      },
    },
    Modal: {
      defaultProps: {
        centered: true,
      },
    },
    Popover: {
      defaultProps: {
        withinPortal: true,
      },
    },
    Notification: {
      defaultProps: {
        radius: 'md',
        withBorder: true,
        withCloseButton: false,
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
  const theme = useMantineTheme()
  const isMobile = useMediaQuery(`(max-width: ${limit ? `${limit}px` : theme.breakpoints.xs})`)
  return isMobile
}

export const ACCEPT_IMAGE_MIME_TYPE = [
  'image/png',
  'image/gif',
  'image/jpeg',
  'image/gif',
  'image/webp',
  'image/bmp',
  'image/tiff',
]

interface FixedButtonProps {
  right?: string
  bottom?: string
}

export const useFixedButtonStyles = createStyles((theme, { right, bottom }: FixedButtonProps) => ({
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

export const useBannerStyles = createStyles((theme) => ({
  root: {
    position: 'relative',
    display: 'flex',
    background: theme.colorScheme === 'dark' ? ` rgba(0,0,0,0.2)` : theme.white,
    justifyContent: 'center',
    backgroundSize: 'cover',
    backgroundPosition: 'center center',
    padding: `calc(${theme.spacing.xl} * 3) 0`,

    [theme.fn.smallerThan('sm')]: {
      justifyContent: 'start',
    },

    [theme.fn.smallerThan('md')]: {
      padding: `calc(${theme.spacing.xl} * 1.5) 1rem`,
    },
  },
  container: {
    position: 'relative',
    maxWidth: '960px',
    width: '100%',
    zIndex: 1,

    [theme.fn.smallerThan('md')]: {
      padding: `${theme.spacing.md} calc(${theme.spacing.md} * 2)`,
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
    fontSize: `calc(${theme.fontSizes.xl} * 2.2)`,
    fontWeight: 900,
    lineHeight: 1.1,

    [theme.fn.smallerThan('md')]: {
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

    [theme.fn.smallerThan('sm')]: {
      display: 'none',
    },
  },
  date: {
    color: theme.colorScheme === 'dark' ? theme.colors.white[0] : theme.colors.gray[6],
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
}))

export const useAccordionStyles = createStyles((theme) => ({
  root: {
    backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[6] : theme.colors.gray[0],
    borderRadius: theme.radius.sm,
  },

  item: {
    backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[6] : theme.colors.gray[0],
    border: '1px solid rgba(0,0,0,0.2)',
    position: 'relative',

    '&[data-active]': {
      backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[7] : theme.white,
      boxShadow: theme.shadows.md,
    },
  },

  label: {
    padding: '0',
  },

  control: {
    padding: '8px 4px',
    ...theme.fn.hover({ background: 'transparent' }),
  },
}))

const FOOTER_HEIGHT = rem(240)

export const useFooterStyles = createStyles((theme) => ({
  spacer: {
    height: FOOTER_HEIGHT,
  },

  wrapper: {
    backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[7] : theme.colors.white[2],
    position: 'fixed',
    zIndex: 5,
    bottom: 0,
    left: 0,
    right: 0,
    height: FOOTER_HEIGHT,
    paddingRight: `calc(${theme.spacing.xl} * 3)`,
    paddingLeft: `calc(var(--mantine-navbar-width) + ${theme.spacing.xl} * 3)`,

    [theme.fn.smallerThan('md')]: {
      padding: `calc(${theme.spacing.xl})`,
    },
  },
}))

export const useLogoStyles = createStyles((theme) => ({
  title: {
    color: theme.colorScheme === 'dark' ? theme.colors.white[0] : theme.colors.gray[6],
  },
  brand: {
    color: theme.colors[theme.primaryColor][4],
  },
  bio: {
    fontFamily: theme.fontFamilyMonospace,
    fontWeight: 'bold',
    fontSize: '1.5rem',
    color: theme.colorScheme === 'dark' ? theme.colors.gray[2] : theme.colors.dark[4],
  },
  blink: {
    animation: `${keyframes`0%, 100% {opacity:0;} 50% {opacity:1;}`} 1s infinite steps(1,start)`,
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

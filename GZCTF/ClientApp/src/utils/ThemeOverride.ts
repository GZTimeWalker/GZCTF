import { createStyles, MantineThemeOverride } from '@mantine/core'

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
      '#03CAAB',
      '#00BFA5',
      '#009985',
      '#007F6E',
    ],
    alert: ['#FF8A8A', '#FF6666', '#FF5252', '#FF3F3F', '#FF2626', '#FF0F0F', '#FF0000'],
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
  fontFamily: "'IBM Plex Sans', sans-serif",
  fontFamilyMonospace: "'JetBrains Mono', monospace",
  headings: {
    fontFamily: "'IBM Plex Sans', sans-serif",
  },
  loader: 'bars',
}

export const useTypographyStyles = createStyles((theme) => ({
  root: {
    '& code': {
      backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors.white[1],
      padding: `1px ${theme.spacing.xs / 2}px`,
      border: 'none',
    },

    '& pre': {
      backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors.white[1],
    },
  },
}))

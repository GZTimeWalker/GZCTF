import { DefaultMantineColor, MantineColorsTuple } from '@mantine/core'

type ExtendedCustomColors =
  | 'brand'
  | 'gray'
  | 'alert'
  | 'light'
  | 'dark'
  | 'custom'
  | DefaultMantineColor

declare module '@mantine/core' {
  export interface MantineThemeColorsOverride {
    colors: Record<ExtendedCustomColors, MantineColorsTuple>
  }
}

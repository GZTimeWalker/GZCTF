import { DefaultMantineColor, Tuple } from '@mantine/core'

type ExtendedCustomColors = 'gray' | 'brand' | 'dark' | 'alert' | 'white' | DefaultMantineColor

declare module '@mantine/core' {
  export interface MantineThemeColorsOverride {
    colors: Record<ExtendedCustomColors, Tuple<string, 10>>
  }
}

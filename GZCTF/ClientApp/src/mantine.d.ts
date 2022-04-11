import { Tuple } from '@mantine/core';

type CustomColors = 'brand' | 'alert';

declare module '@mantine/core' {
  export interface MantineThemeColorsOverride {
    colors: Record<CustomColors, Tuple<string, 10>>;
  }
}

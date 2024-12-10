import { StyleProp, rem } from '@mantine/core'
import { FC, SVGProps } from 'react'
import classes from '@Styles/Icon.module.css'

export interface MainIconProps {
  ignoreTheme?: boolean
  size?: StyleProp<React.CSSProperties['width']>
}

export const MainIcon: FC<MainIconProps & SVGProps<SVGSVGElement>> = ({ ignoreTheme, size, ...svgProps }) => {
  return (
    <svg
      width="480"
      height="480"
      viewBox="0 0 4800 4800"
      style={{
        marginLeft: `calc(${rem(size)} / 10)`,
        width: rem(size) || 'auto',
        height: 'auto',
        aspectRatio: '1 / 1',
      }}
      {...svgProps}
    >
      {ignoreTheme ? (
        <path
          fill="#fff"
          fillRule="evenodd"
          d="M2994.48,4244.61L505.28,2807.47V1992.53l256.572-148.14L1287,2285l258-307,160.39,135.56L1209.27,2400l1786.1,1031.21V2427.79L3517,1806,2420.98,886.5l573.5-331.11,705.76,407.474V3837.14Z"
        />
      ) : (
        <path
          className={classes.main}
          fillRule="evenodd"
          d="M2994.48,4244.61L505.28,2807.47V1992.53l256.572-148.14L1287,2285l258-307,160.39,135.56L1209.27,2400l1786.1,1031.21V2427.79L3517,1806,2420.98,886.5l573.5-331.11,705.76,407.474V3837.14Z"
        />
      )}
      <g id="Flag">
        <path
          id="Flag_0"
          className={classes.front}
          fillRule="evenodd"
          d="M1280.55,582.029L2046.6,1224.82l-771.35,919.25L509.21,1501.28Z"
        />
        <path
          id="Flag_1"
          className={classes.back}
          fillRule="evenodd"
          d="M1225.95,1580.54l306.42,257.11-257.12,306.42Z"
        />
        <path
          id="Flag_2"
          className={classes.mid}
          fillRule="evenodd"
          d="M2636.97,2699.25l-32.14,38.31-264.98,315.78L1812.4,2748.51l332.8-396.63-919.25-771.34L1997.3,661.284,3376.18,1818.3ZM1880,3601.24l0.15-.04L1351.4,4231.34,891.769,3845.67l460.361-548.64Z"
        />
      </g>
    </svg>
  )
}

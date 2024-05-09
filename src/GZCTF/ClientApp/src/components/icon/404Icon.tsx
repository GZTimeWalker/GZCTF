import { createStyles, keyframes } from '@mantine/emotion'
import { FC } from 'react'
import { useIconStyles } from '@Utils/ThemeOverride'

const spinning = keyframes({
  from: { transform: 'rotate(0deg)' },
  to: { transform: 'rotate(360deg)' },
})

const useStyles = createStyles((theme, _, u) => ({
  triangle: {
    transformOrigin: '50% 50%',
    animation: `${spinning} 8s linear infinite`,

    [u.dark]: {
      fill: theme.white,
    },

    [u.light]: {
      fill: theme.colors.gray[6],
    },
  },
}))

const Icon404: FC = () => {
  const { classes: triClasses } = useStyles()
  const { classes } = useIconStyles()
  return (
    <svg
      id="main_logo"
      xmlns="http://www.w3.org/2000/svg"
      width="320"
      height="320"
      viewBox="0 0 6400 6400"
    >
      <g id="Num4_left">
        <path
          className={classes.back}
          fillRule="evenodd"
          d="M5000,2400l282.84,282.84-800.11,800.11-282.84-282.84Z"
        />
        <rect className={classes.mid} x="4200" y="3200" width="1600" height="400" />
        <rect className={classes.front} x="5000" y="2400" width="400" height="1600" />
      </g>

      <path
        id="Triangle"
        className={triClasses.triangle}
        fillRule="evenodd"
        d="M3794.48,5044.61L1305.28,3607.47V2792.53l2489.2-1437.14,705.76,407.47V4637.14Zm0.89-2875.82L2009.27,3200l1786.1,1031.21V2168.79Z"
      />

      <path
        id="Num0"
        className={classes.front}
        fillRule="evenodd"
        d="M3200,2400c441.83,0,800,358.17,800,800s-358.17,800-800,800-800-358.17-800-800S2758.17,2400,3200,2400Zm0,400c220.91,0,400,179.09,400,400s-179.09,400-400,400-400-179.09-400-400S2979.09,2800,3200,2800Z"
      />

      <path
        id="Triangle_front"
        className={triClasses.triangle}
        fillRule="evenodd"
        d="M3795,4230.99l0.37,0.22V2168.79l-0.37.22V1355.69l705.24,407.17V4637.14L3795,5044.31V4230.99Z"
      />

      <g id="Num4_right">
        <path
          className={classes.back}
          fillRule="evenodd"
          d="M1200,2400l282.84,282.84L682.732,3482.95,399.89,3200.11Z"
        />
        <rect className={classes.mid} x="400" y="3200" width="1600" height="400" />
        <rect className={classes.front} x="1200" y="2400" width="400" height="1600" />
      </g>
    </svg>
  )
}

export default Icon404

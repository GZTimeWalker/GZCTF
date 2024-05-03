import { FC } from 'react'
import { useIconStyles } from '@Utils/ThemeOverride'

const IconWiderScreenRequired: FC = () => {
  const { classes } = useIconStyles()

  return (
    <svg
      id="IconWiderScreenRequired"
      xmlns="http://www.w3.org/2000/svg"
      width="320"
      height="320"
      viewBox="0 0 6400 6400"
    >
      <path id="arrowRodL2" fill="#007f6e" fillRule="evenodd" d="M1920,2900v600H640V2900H1920Z" />
      <path id="arrowRodL1" fill="#00bfa5" fillRule="evenodd" d="M3200,2900v600H1920V2900H3200Z" />
      <path
        id="Triangle"
        className={classes.triangle}
        fillRule="evenodd"
        d="M3794.48,5044.61L1305.28,3607.47V2792.53l2489.2-1437.14,705.76,407.47V4637.14Zm0.89-2875.82L2009.27,3200l1786.1,1031.21V2168.79Z"
      />
      <path id="arrowRodR" fill="#00bfa5" fillRule="evenodd" d="M3200,3500V2900H5760v600H3200Z" />
      <path
        id="arrowR"
        fill="#1de9b6"
        fillRule="evenodd"
        d="M6400,3200L5268.63,4331.37l-424.27-424.26L5551.47,3200l-707.11-707.11,424.27-424.26,707.11,707.11h0Z"
      />
      <path
        id="arrowL"
        fill="#00bfa5"
        fillRule="evenodd"
        d="M1555.63,3907.11l-424.26,424.26L0,3200l424.264-424.26h0l707.106-707.11,424.26,424.26L848.528,3200Z"
      />
    </svg>
  )
}

export default IconWiderScreenRequired

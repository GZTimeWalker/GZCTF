import { FC } from 'react'
import { useIconStyles } from '@Utils/ThemeOverride'

const IconWarning: FC = () => {
  const { classes } = useIconStyles()

  return (
    <svg
      id="Warning"
      xmlns="http://www.w3.org/2000/svg"
      width="200"
      height="200"
      viewBox="0 0 4000 4000"
    >
      <path id="Rect" fill="#dd5e5e" d="M1600,2084V222h720V2084H1600Z" />
      <path
        id="Triangle"
        className={classes.triangle}
        d="M2834.48,3844.61L345.28,2407.47V1592.53L2834.48,155.387l705.76,407.474V3437.14Zm0.89-2875.816L1049.27,2000l1786.1,1031.21V968.794Z"
      />
      <path id="Parallelogram" fill="#dd5e5e" d="M1600,1999.85l720-415.7v782l-720,415.7v-782Z" />
      <circle id="Circle" fill="#cd3535" cx="1960" cy="3337" r="391" />
    </svg>
  )
}

export default IconWarning

import { Box, BoxProps, ElementProps, em } from '@mantine/core'
import { forwardRef } from 'react'
import MainIcon, { MainIconProps } from '@Components/icon/MainIcon'
import { useConfig } from '@Utils/useConfig'

type LogoProps = MainIconProps & BoxProps & ElementProps<'div'>

export const LogoBox = forwardRef<HTMLDivElement, LogoProps>((props, ref) => {
  const { size, ignoreTheme, ...others } = props
  const { config } = useConfig()

  config // TODO

  return (
    <Box {...others} ref={ref} w={size} h={size}>
      <MainIcon ignoreTheme={ignoreTheme} size={em(size)} />
    </Box>
  )
})

export default LogoBox

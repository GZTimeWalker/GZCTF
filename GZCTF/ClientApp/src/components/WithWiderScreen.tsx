import { FC } from 'react'
import { Stack, Title, Text } from '@mantine/core'
import { useViewportSize } from '@mantine/hooks'
import IconWiderScreenRequired from './icon/WiderScreenRequiredIcon'

interface WithWiderScreenProps extends React.PropsWithChildren {
  minWidth?: number
}
// 580 可以在桌面模式下完成注册流程
const WithWiderScreen: FC<WithWiderScreenProps> = ({ children, minWidth = 580 }) => {
  const view = useViewportSize()

  const tooSmall = minWidth > 0 && view.width > 0 && view.width < minWidth

  return tooSmall ? (
    <Stack spacing={0} align="center" justify="center" style={{ height: 'calc(100vh - 32px)' }}>
      <IconWiderScreenRequired />
      <Title order={1} color="#00bfa5" style={{ fontWeight: 'lighter' }}>
        页面宽度不足
      </Title>
      <Text style={{ fontWeight: 'bold' }}>请使用更宽的设备或桌面模式浏览本页面</Text>
    </Stack>
  ) : (
    <>{children}</>
  )
}

export default WithWiderScreen

import { FC } from 'react'
import { Group, Header, useMantineTheme } from '@mantine/core'
import { useViewportSize } from '@mantine/hooks'
import LogoHeader from './LogoHeader'

const AppHeader: FC = () => {
  const theme = useMantineTheme()
  const view = useViewportSize()

  const showHeader = view.width > 0 && view.width < theme.breakpoints.xs

  return (
    <Header
      hidden={!showHeader}
      height={showHeader ? 70 : 0}
      fixed
      sx={(theme) => ({ width: '100%', backgroundColor: theme.colors.gray[8] })}
    >
      <Group style={{ height: '100%' }} p="0 1rem">
        <LogoHeader />
      </Group>
    </Header>
  )
}

export default AppHeader

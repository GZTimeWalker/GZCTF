import { FC } from 'react'
import { createStyles, Group, Header } from '@mantine/core'
import { useViewportSize } from '@mantine/hooks'
import LogoHeader from './LogoHeader'

const useStyles = createStyles((theme) => {
  return {
    header: {
      backgroundColor: theme.colors.gray[8],

      [theme.fn.largerThan('xs')]: {
        display: 'none',
      },
    },
  }
})

const AppHeader: FC = () => {
  const { classes, theme } = useStyles()
  const view = useViewportSize()

  const showHeader = view.width < theme.breakpoints.xs

  return (
    <Header height={showHeader ? 70 : 0} className={classes.header}>
      <Group style={{ height: '100%' }} p="0 1rem">
        <LogoHeader />
      </Group>
    </Header>
  )
}

export default AppHeader

import { FC } from 'react'
import { Group, Title, createStyles } from '@mantine/core'
import MainIcon from './icon/MainIcon'

const useStyles = createStyles((theme) => ({
  brand: {
    color: theme.colorScheme === 'dark'
      ? theme.colors[theme.primaryColor][4]
      : theme.colors[theme.primaryColor][6],
  },
  title: {
    color: theme.colorScheme === 'dark' ? "#fff" : "#414141",
    marginLeft: '-20px',
  },
}))

const LogoHeader: FC = () => {
  const { classes, cx } = useStyles()

  return (
    <Group>
      <MainIcon style={{ maxWidth: 60, height: 'auto' }} />
      <Title className={cx(classes.title)}>
        GZ<span className={cx(classes.brand)} style={{ display: "inline-block", transform: "translateY(-.12ch)" }}>::</span>CTF
      </Title>
    </Group>
  )
}

export default LogoHeader

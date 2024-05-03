import { Group, GroupProps, Title } from '@mantine/core'
import { createStyles } from '@mantine/emotion'
import { forwardRef } from 'react'
import MainIcon from '@Components/icon/MainIcon'
import { useConfig } from '@Utils/useConfig'

const useStyles = createStyles((theme, _, u) => ({
  brand: {
    display: 'inline-block',

    [u.dark]: {
      color: theme.colors[theme.primaryColor][4],
    },

    [u.light]: {
      color: theme.colors[theme.primaryColor][6],
    },
  },
  title: {
    marginLeft: '-20px',

    [u.dark]: {
      color: theme.colors.light[0],
    },

    [u.light]: {
      color: theme.colors.gray[6],
    },
  },
}))

export const LogoHeader = forwardRef<HTMLDivElement, GroupProps>((props, ref) => {
  const { classes } = useStyles()
  const { config } = useConfig()
  return (
    <Group ref={ref} wrap="nowrap" {...props}>
      <MainIcon style={{ maxWidth: 60, height: 'auto' }} />
      <Title className={classes.title}>
        {config?.title ?? 'GZ'}
        <span className={classes.brand}>::</span>CTF
      </Title>
    </Group>
  )
})

export default LogoHeader

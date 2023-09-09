import { FC } from 'react'
import { createStyles, Group, keyframes, Text, Title } from '@mantine/core'
import LogoHeader from '@Components/LogoHeader'
import { useConfig } from '@Utils/useConfig'

const useStyles = createStyles((theme) => ({
  group: {
    width: '100%',
    top: 16,
    paddingBottom: 8,
    position: 'sticky',
    zIndex: 50,

    '::before': {
      content: '""',
      position: 'absolute',
      top: -16,
      right: 0,
      left: 0,
      paddingTop: 16,
    },

    [theme.fn.smallerThan('xs')]: {
      display: 'none',
    },
  },
  blink: {
    animation: `${keyframes`0%, 100% {
                              opacity: 0;
                            }
                              50% {
                                opacity: 1;
                              }`} 1s infinite steps(1,start)`,
  },
  subtitle: {
    fontFamily: theme.fontFamilyMonospace,
    color: theme.colorScheme === 'dark' ? theme.colors.gray[4] : theme.colors.dark[4],

    [theme.fn.smallerThan(900)]: {
      display: 'none',
    },
  },
}))

const StickyHeader: FC = () => {
  const { classes } = useStyles()
  const { config } = useConfig()

  return (
    <Group position="apart" align="flex-end" className={classes.group}>
      <LogoHeader />
      <Title className={classes.subtitle} order={3}>
        &gt; {config?.slogan ?? 'Hack for fun not for profit'}
        <Text span className={classes.blink}>
          _
        </Text>
      </Title>
    </Group>
  )
}

export default StickyHeader

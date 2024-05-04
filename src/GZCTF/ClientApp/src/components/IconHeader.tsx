import { Box, Group, Text, Title } from '@mantine/core'
import { createStyles, keyframes } from '@mantine/emotion'
import { FC } from 'react'
import LogoHeader from '@Components/LogoHeader'
import { useIsMobile } from '@Utils/ThemeOverride'
import { useConfig } from '@Utils/useConfig'

interface StickyHeaderProps {
  sticky?: boolean
}

const useStyles = createStyles((theme, { sticky }: StickyHeaderProps, u) => ({
  group: {
    width: '100%',
    top: 16,
    paddingBottom: 8,
    position: sticky ? 'sticky' : 'relative',
    zIndex: 50,

    // only show before on sticky
    ...(sticky && {
      '&::before': {
        content: '""',
        position: 'absolute',
        top: -16,
        right: 0,
        left: 0,
        paddingTop: 16,
      },
    }),
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
    [u.dark]: {
      color: theme.colors.gray[4],
    },

    [u.light]: {
      color: theme.colors.dark[4],
    },
  },
}))

const IconHeader: FC<StickyHeaderProps> = (props) => {
  const { classes } = useStyles(props)
  const { config } = useConfig()
  const isMobile = useIsMobile()

  return isMobile ? (
    <Box h={8} />
  ) : (
    <Group justify="space-between" align="flex-end" className={classes.group}>
      <LogoHeader />
      <Title className={classes.subtitle} order={3} ff="monospace">
        &gt; {config?.slogan ?? 'Hack for fun not for profit'}
        <Text span ff="monospace" fw="bold" className={classes.blink}>
          _
        </Text>
      </Title>
    </Group>
  )
}

export default IconHeader

import { Box, Group, Text, Title } from '@mantine/core'
import { createStyles, keyframes } from '@mantine/emotion'
import { FC } from 'react'
import LogoHeader from '@Components/LogoHeader'
import { useIsMobile } from '@Utils/ThemeOverride'
import { useConfig } from '@Utils/useConfig'

interface StickyHeaderProps {
  sticky?: boolean
  px?: string
}

const useStyles = createStyles((theme, { sticky, px }: StickyHeaderProps, u) => ({
  group: {
    width: '100%',
    top: 16,
    paddingBottom: 8,
    position: sticky ? 'sticky' : 'relative',
    zIndex: 50,
    padding: `0 ${px ?? 0} ${theme.spacing.sm} ${px ?? 0}`,

    [u.dark]: {
      background: theme.colors.gray[7],
    },

    [u.light]: {
      background: theme.colors.light[2],
    },

    // make it wider to cover the card shadow on sticky
    // only show before on sticky
    ...(sticky && {
      '&::before': {
        content: '""',
        position: 'absolute',
        top: -16,
        right: 0,
        left: 0,
        paddingTop: 16,

        [u.dark]: {
          background: theme.colors.gray[7],
        },

        [u.light]: {
          background: theme.colors.light[2],
        },
      },
    }),
  },
  blink: {
    fontFamily: theme.fontFamilyMonospace,
    fontWeight: 'bold',
    fontSize: '1.375rem',

    animation: `${keyframes`0%, 100% {
                              opacity: 0;
                            }
                              50% {
                                opacity: 1;
                              }`} 1s infinite steps(1,start)`,
  },
  subtitle: {
    fontFamily: theme.fontFamilyMonospace,
    fontWeight: 'bold',

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
      <Title className={classes.subtitle} order={3}>
        &gt; {config?.slogan ?? 'Hack for fun not for profit'}
        <Text span className={classes.blink}>
          _
        </Text>
      </Title>
    </Group>
  )
}

export default IconHeader

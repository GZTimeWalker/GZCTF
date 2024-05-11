import { Box, Group, Text, Title } from '@mantine/core'
import { FC } from 'react'
import LogoHeader from '@Components/LogoHeader'
import { useIsMobile } from '@Utils/ThemeOverride'
import { useConfig } from '@Utils/useConfig'
import classes from '@Styles/IconHeader.module.css'

interface StickyHeaderProps {
  sticky?: boolean
  px?: string
}

const IconHeader: FC<StickyHeaderProps> = ({ sticky, px }) => {
  const { config } = useConfig()
  const isMobile = useIsMobile()

  return isMobile ? (
    <Box h={8} />
  ) : (
    <Group
      __vars={{
        '--header-px': px || undefined,
      }}
      data-sticky={sticky || undefined}
      className={classes.group}
    >
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

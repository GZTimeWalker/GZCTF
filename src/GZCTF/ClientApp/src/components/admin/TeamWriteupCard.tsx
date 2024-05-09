import {
  ActionIcon,
  Avatar,
  Card,
  Group,
  CardProps,
  Stack,
  Text,
  useMantineColorScheme,
} from '@mantine/core'
import { mdiDownload } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { useHoverCardStyles } from '@Utils/ThemeOverride'
import { WriteupInfoModel } from '@Api'

interface TeamWriteupCardProps extends CardProps {
  writeup: WriteupInfoModel
  selected?: boolean
  onClick: () => void
}

const TeamWriteupCard: FC<TeamWriteupCardProps> = ({ writeup, selected, ...props }) => {
  const { colorScheme } = useMantineColorScheme()
  const { classes, theme } = useHoverCardStyles()
  return (
    <Card
      {...props}
      p="sm"
      shadow="sm"
      classNames={classes}
      style={{
        border: `2px solid ${
          selected
            ? theme.colors[theme.primaryColor][colorScheme === 'dark' ? 8 : 6]
            : 'transparent'
        }`,
      }}
    >
      <Group wrap="nowrap" gap={3} justify="space-between">
        <Group gap="sm" wrap="nowrap" justify="space-between">
          <Avatar alt="avatar" src={writeup.team?.avatar} size="md">
            {writeup.team?.name?.slice(0, 1)}
          </Avatar>
          <Stack gap={0}>
            <Text size="0.8rem" lineClamp={1} c="dimmed">
              #{writeup.team?.id}
            </Text>
            <Text lineClamp={1} fw={600}>
              {writeup.team?.name}
            </Text>
            <Text size="xs" lineClamp={1} c="dimmed">
              {dayjs(writeup.uploadTimeUtc).format('YY/MM/DD HH:mm')}
            </Text>
          </Stack>
        </Group>
        <ActionIcon onClick={() => window.open(writeup.url, '_blank')}>
          <Icon path={mdiDownload} size={1} />
        </ActionIcon>
      </Group>
    </Card>
  )
}

export default TeamWriteupCard

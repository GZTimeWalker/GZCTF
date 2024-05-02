import { ActionIcon, Avatar, Card, Group, PaperProps, Stack, Text } from '@mantine/core'
import { mdiDownload } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { WriteupInfoModel } from '@Api'

interface TeamWriteupCardProps extends PaperProps {
  writeup: WriteupInfoModel
  selected?: boolean
  onClick: () => void
}

const TeamWriteupCard: FC<TeamWriteupCardProps> = ({ writeup, selected, ...props }) => {
  return (
    <Card
      {...props}
      p="sm"
      shadow="sm"
      sx={(theme) => ({
        border: `2px solid ${
          selected ? theme.colors.brand[colorScheme === 'dark' ? 8 : 6] : 'transparent'
        }`,
        cursor: 'pointer',
      })}
    >
      <Group wrap="nowrap" gap={3} justify="space-between">
        <Group wrap="nowrap" justify="space-between">
          <Avatar alt="avatar" src={writeup.team?.avatar} size="md">
            {writeup.team?.name?.slice(0, 1)}
          </Avatar>
          <Stack gap={0}>
            <Text lineClamp={1} fw={600}>
              {writeup.team?.name}
            </Text>
            <Text lineClamp={1} size="xs" c="dimmed">
              {dayjs(writeup.uploadTimeUtc).format('YYYY-MM-DD HH:mm')}
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

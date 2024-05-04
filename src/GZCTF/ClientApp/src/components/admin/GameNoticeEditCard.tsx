import { ActionIcon, Card, Group, CardProps, Stack, Text } from '@mantine/core'
import { mdiDeleteOutline, mdiPencilOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { InlineMarkdownRender } from '@Components/MarkdownRender'
import { GameNotice } from '@Api'

interface GameNoticeEditCardProps extends CardProps {
  gameNotice: GameNotice
  onDelete: () => void
  onEdit: () => void
}

const GameNoticeEditCard: FC<GameNoticeEditCardProps> = ({
  gameNotice,
  onDelete,
  onEdit,
  ...props
}) => {
  return (
    <Card {...props} shadow="sm" p="sm">
      <Group justify="space-between" wrap="nowrap">
        <Stack gap={1}>
          <InlineMarkdownRender source={gameNotice.values.at(-1) || ''} />
          <Text size="xs" fw="bold" c="dimmed">
            {dayjs(gameNotice.time).format('#YY/MM/DD HH:mm:ss')}
          </Text>
        </Stack>
        <Group justify="right" wrap="nowrap">
          <ActionIcon onClick={onEdit}>
            <Icon path={mdiPencilOutline} size={1} />
          </ActionIcon>
          <ActionIcon onClick={onDelete} color="red">
            <Icon path={mdiDeleteOutline} size={1} />
          </ActionIcon>
        </Group>
      </Group>
    </Card>
  )
}

export default GameNoticeEditCard

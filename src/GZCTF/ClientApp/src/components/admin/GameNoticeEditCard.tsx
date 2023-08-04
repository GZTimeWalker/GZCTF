import dayjs from 'dayjs'
import { FC } from 'react'
import { ActionIcon, Group, Card, PaperProps, Stack, Text } from '@mantine/core'
import { mdiPencilOutline, mdiDeleteOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { InlineMarkdownRender } from '@Components/MarkdownRender'
import { GameNotice } from '@Api'

interface GameNoticeEditCardProps extends PaperProps {
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
      <Group position="apart" noWrap>
        <Stack spacing={1}>
          <InlineMarkdownRender source={gameNotice.content} />
          <Text size="xs" fw={700} c="dimmed">
            {dayjs(gameNotice.time).format('#YY/MM/DD HH:mm:ss')}
          </Text>
        </Stack>
        <Group position="right" noWrap>
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

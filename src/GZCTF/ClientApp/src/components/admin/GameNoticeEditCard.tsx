import { FC } from 'react'
import { Text, ActionIcon, Group, Card, PaperProps } from '@mantine/core'
import { mdiPencilOutline, mdiDeleteOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { GameNotice } from '../../Api'

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
    <Card {...props} shadow="sm" p="lg">
      <Group position="apart">
        <Group position="left">
          <Text weight="500">{gameNotice.content}</Text>
        </Group>
        <Group position="right">
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

import { marked } from 'marked'
import { FC } from 'react'
import {
  Text,
  ActionIcon,
  Group,
  Stack,
  Badge,
  Card,
  Title,
  useMantineTheme,
  TypographyStylesProvider,
} from '@mantine/core'
import { mdiPencilOutline, mdiDeleteOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { GameNotice } from '../Api'

interface GameNoticeEditCardProps {
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
  const theme = useMantineTheme()
  return (
    <Card shadow="sm" p="lg" {...props}>
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

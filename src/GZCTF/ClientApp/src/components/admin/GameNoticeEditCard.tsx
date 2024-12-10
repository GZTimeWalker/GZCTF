import { ActionIcon, Card, Group, CardProps, Stack, Text } from '@mantine/core'
import { mdiDeleteOutline, mdiPencilOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { InlineMarkdown } from '@Components/MarkdownRenderer'
import { useLanguage } from '@Utils/I18n'
import { GameNotice } from '@Api'

interface GameNoticeEditCardProps extends CardProps {
  gameNotice: GameNotice
  onDelete: () => void
  onEdit: () => void
}

export const GameNoticeEditCard: FC<GameNoticeEditCardProps> = ({ gameNotice, onDelete, onEdit, ...props }) => {
  const { locale } = useLanguage()

  return (
    <Card {...props} shadow="sm" p="sm">
      <Group justify="space-between" wrap="nowrap">
        <Stack gap={1}>
          <InlineMarkdown source={gameNotice.values.at(-1) || ''} />
          <Text size="xs" fw="bold" c="dimmed">
            {dayjs(gameNotice.time).locale(locale).format('#SLL LTS')}
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

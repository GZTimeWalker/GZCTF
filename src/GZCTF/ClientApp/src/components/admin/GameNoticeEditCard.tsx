import { ActionIcon, Badge, Card, Group, CardProps, Stack, Text } from '@mantine/core'
import { mdiCalendarClock, mdiDeleteOutline, mdiPencilOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
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
  const { t } = useTranslation()
  const isScheduled = new Date(gameNotice.time) > new Date()

  return (
    <Card {...props} shadow="sm" p="sm" style={isScheduled ? { opacity: 0.7 } : undefined}>
      <Group justify="space-between" wrap="nowrap">
        <Stack gap={1}>
          <InlineMarkdown source={gameNotice.values.at(-1) || ''} />
          <Group gap="xs">
            <Text size="xs" fw="bold" c="dimmed">
              {dayjs(gameNotice.time).locale(locale).format('#SLL LTS')}
            </Text>
            {isScheduled && (
              <Badge
                size="xs"
                color="orange"
                variant="light"
                leftSection={<Icon path={mdiCalendarClock} size={0.5} />}
              >
                {t('admin.label.games.notices.scheduled_badge')}
              </Badge>
            )}
          </Group>
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

import { ActionIcon, Card, Group, Stack, Text } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import type { SkillTreeContentSummaryResponse } from '@Api'

type CategoryContentListProps = {
  contents: SkillTreeContentSummaryResponse[]
  onMove: (from: number, to: number) => void
  disabled?: boolean
}

export const CategoryContentList = ({ contents, onMove, disabled }: CategoryContentListProps) => {
  const { t } = useTranslation('skillTrees')

  if (contents.length === 0)
    return <Text c="dimmed">{t('editor.emptyPreview')}</Text>

  return (
    <Stack gap="xs">
      {contents.map((content, index) => (
        <Card key={`${content.kind}-${content.contentId}`} withBorder padding="xs">
          <Group justify="space-between" wrap="nowrap">
            <Stack gap={0}>
              <Group gap="xs">
                <Text size="xs" c="dimmed">
                  {content.kind}
                </Text>
                <Text size="xs" c="dimmed">
                  {content.state ? t(`state.${content.state.toLowerCase()}`) : ''}
                </Text>
              </Group>
              <Text fw={500}>{content.title}</Text>
            </Stack>
            <Group gap={4} wrap="nowrap">
              <ActionIcon
                variant="subtle"
                aria-label={`${t('editor.moveUp')}: ${content.title}`}
                disabled={disabled || index === 0}
                onClick={() => onMove(index, index - 1)}
              >
                ↑
              </ActionIcon>
              <ActionIcon
                variant="subtle"
                aria-label={`${t('editor.moveDown')}: ${content.title}`}
                disabled={disabled || index === contents.length - 1}
                onClick={() => onMove(index, index + 1)}
              >
                ↓
              </ActionIcon>
            </Group>
          </Group>
        </Card>
      ))}
    </Stack>
  )
}

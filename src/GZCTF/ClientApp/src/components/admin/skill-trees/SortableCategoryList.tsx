import { ActionIcon, Card, Group, Stack, Text } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { skillTreeIcons, type SkillTreeIconKey } from '@Utils/skillTreeAdmin'

export type DraftCategory = {
  categoryId: string
  name: string
  summary: string
  iconKey: SkillTreeIconKey
  rowVersion: number
  sortOrder: number
}

type SortableCategoryListProps = {
  categories: DraftCategory[]
  onMove: (from: number, to: number) => void
  onRemove: (categoryId: string) => void
  disabled?: boolean
}

export const SortableCategoryList = ({
  categories,
  onMove,
  onRemove,
  disabled,
}: SortableCategoryListProps) => {
  const { t } = useTranslation('skillTrees')

  if (categories.length === 0)
    return <Text c="dimmed">{t('editor.emptyCategories')}</Text>

  return (
    <Stack gap="xs">
      {categories.map((category, index) => (
        <Card key={category.categoryId} withBorder padding="sm">
          <Group justify="space-between" wrap="nowrap">
            <Group gap="xs" wrap="nowrap">
              <Text size="lg" aria-hidden>
                {skillTreeIcons[category.iconKey] ?? '🚩'}
              </Text>
              <Stack gap={0}>
                <Text fw={500}>{category.name}</Text>
                {category.summary && (
                  <Text size="xs" c="dimmed" lineClamp={1}>
                    {category.summary}
                  </Text>
                )}
              </Stack>
            </Group>
            <Group gap={4} wrap="nowrap">
              <ActionIcon
                variant="subtle"
                aria-label={`${t('editor.moveUp')}: ${category.name}`}
                disabled={disabled || index === 0}
                onClick={() => onMove(index, index - 1)}
              >
                ↑
              </ActionIcon>
              <ActionIcon
                variant="subtle"
                aria-label={`${t('editor.moveDown')}: ${category.name}`}
                disabled={disabled || index === categories.length - 1}
                onClick={() => onMove(index, index + 1)}
              >
                ↓
              </ActionIcon>
              <ActionIcon
                variant="subtle"
                color="red"
                aria-label={`${t('editor.remove')}: ${category.name}`}
                disabled={disabled}
                onClick={() => onRemove(category.categoryId)}
              >
                ×
              </ActionIcon>
            </Group>
          </Group>
        </Card>
      ))}
      <Text size="xs" c="dimmed">
        {t('editor.removeHint')}
      </Text>
    </Stack>
  )
}

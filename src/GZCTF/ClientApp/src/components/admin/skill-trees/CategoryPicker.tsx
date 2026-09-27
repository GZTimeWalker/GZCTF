import { Button, Card, Group, ScrollArea, Stack, Text, TextInput } from '@mantine/core'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { SkillCategoryAdminResponse } from '@Api'
import { skillTreeIcons, type SkillTreeIconKey } from '@Utils/skillTreeAdmin'

type CategoryPickerProps = {
  categories: SkillCategoryAdminResponse[]
  selectedIds: string[]
  onSelect: (category: SkillCategoryAdminResponse) => void
  disabled?: boolean
}

export const CategoryPicker = ({ categories, selectedIds, onSelect, disabled }: CategoryPickerProps) => {
  const { t } = useTranslation('skillTrees')
  const [search, setSearch] = useState('')

  const keyword = search.trim().toLowerCase()
  const visible = categories.filter((category) => !keyword || category.name?.toLowerCase().includes(keyword))

  return (
    <Stack gap="xs">
      <TextInput
        label={t('editor.searchCategory')}
        value={search}
        disabled={disabled}
        onChange={(event) => setSearch(event.currentTarget.value)}
      />
      <ScrollArea.Autosize mah={260} type="auto">
        <Stack gap="xs">
          {visible.length === 0 && (
            <Text size="sm" c="dimmed">
              {t('editor.noCategories')}
            </Text>
          )}
          {visible.map((category) => {
            const selected = selectedIds.includes(category.categoryId ?? '')
            return (
              <Card key={category.categoryId} withBorder padding="xs">
                <Group justify="space-between" wrap="nowrap">
                  <Group gap="xs" wrap="nowrap">
                    <Text size="lg" aria-hidden>
                      {skillTreeIcons[(category.iconKey ?? 'flag') as SkillTreeIconKey] ?? '🚩'}
                    </Text>
                    <Text fw={500}>{category.name}</Text>
                  </Group>
                  <Button
                    size="compact-sm"
                    variant={selected ? 'default' : 'light'}
                    disabled={disabled || selected}
                    onClick={() => onSelect(category)}
                  >
                    {selected ? t('category.inTree') : t('editor.addCategory')}
                  </Button>
                </Group>
              </Card>
            )
          })}
        </Stack>
      </ScrollArea.Autosize>
    </Stack>
  )
}

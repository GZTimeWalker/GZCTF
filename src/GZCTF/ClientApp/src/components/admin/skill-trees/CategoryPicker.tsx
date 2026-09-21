import { Button, Card, Collapse, Group, ScrollArea, Stack, Text, TextInput } from '@mantine/core'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { SkillCategoryAdminResponse } from '@Api'
import { PresetIconPicker } from '@Components/admin/skill-trees/PresetIconPicker'
import { skillTreeIcons, type SkillTreeIconKey } from '@Utils/skillTreeAdmin'

export type NewCategoryValues = {
  name: string
  summary: string
  iconKey: SkillTreeIconKey
}

type CategoryPickerProps = {
  categories: SkillCategoryAdminResponse[]
  selectedIds: string[]
  onSelect: (category: SkillCategoryAdminResponse) => void
  onCreate: (values: NewCategoryValues) => Promise<void>
  creating?: boolean
  disabled?: boolean
}

export const CategoryPicker = ({
  categories,
  selectedIds,
  onSelect,
  onCreate,
  creating,
  disabled,
}: CategoryPickerProps) => {
  const { t } = useTranslation('skillTrees')
  const [search, setSearch] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [newCategory, setNewCategory] = useState<NewCategoryValues>({ name: '', summary: '', iconKey: 'flag' })

  const keyword = search.trim().toLowerCase()
  const visible = categories.filter((category) => !keyword || category.name?.toLowerCase().includes(keyword))

  const submitCreate = async () => {
    if (!newCategory.name.trim()) return
    await onCreate({ ...newCategory, name: newCategory.name.trim(), summary: newCategory.summary.trim() })
    setNewCategory({ name: '', summary: '', iconKey: 'flag' })
    setShowCreate(false)
  }

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
      <Button variant="subtle" w="fit-content" disabled={disabled} onClick={() => setShowCreate((value) => !value)}>
        {t('editor.createCategory')}
      </Button>
      <Collapse expanded={showCreate}>
        <Stack>
          <TextInput
            label={t('editor.newCategoryName')}
            value={newCategory.name}
            disabled={disabled}
            onChange={(event) => setNewCategory({ ...newCategory, name: event.currentTarget.value })}
          />
          <TextInput
            label={t('editor.newCategorySummary')}
            value={newCategory.summary}
            disabled={disabled}
            onChange={(event) => setNewCategory({ ...newCategory, summary: event.currentTarget.value })}
          />
          <PresetIconPicker
            value={newCategory.iconKey}
            disabled={disabled}
            onChange={(iconKey) => setNewCategory({ ...newCategory, iconKey })}
          />
          <Button loading={creating} w="fit-content" onClick={submitCreate}>
            {t('editor.createCategory')}
          </Button>
        </Stack>
      </Collapse>
    </Stack>
  )
}

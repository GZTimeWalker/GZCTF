import { Badge, Button, Card, Group, Modal, SimpleGrid, Stack, Text } from '@mantine/core'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { SkillTreeForm, type SkillTreeFormValues } from '@Components/admin/skill-trees/SkillTreeForm'
import { useAdminSkillCategories, useSkillTreeAdminMutations } from '@Hooks/useSkillTreeAdmin'
import { skillTreeIcons, type SkillTreeIconKey } from '@Utils/skillTreeAdmin'
import { showSkillTreeError } from '@Utils/skillTreeAdminFeedback'
import classes from '@Components/admin/workspace/AdminWorkspace.module.css'
import type { CreatePanelProps } from '@Components/admin/workspace/types'

export const CategoriesPanel = ({ createOpen, onClose, onOpen }: CreatePanelProps) => {
  const { t } = useTranslation('skillTrees')
  const { data: categories, isLoading: categoriesLoading } = useAdminSkillCategories()
  const { createCategory } = useSkillTreeAdminMutations()
  const navigate = useNavigate()

  const [values, setValues] = useState<SkillTreeFormValues>({ name: '', summary: '', iconKey: 'flag' })
  const [nameError, setNameError] = useState<string>()
  const [creating, setCreating] = useState(false)

  const create = async () => {
    const name = values.name.trim()
    if (!name) {
      setNameError(t('form.nameRequired'))
      return
    }

    setNameError(undefined)
    setCreating(true)
    try {
      const result = await createCategory({ name, summary: values.summary.trim(), iconKey: values.iconKey })
      onClose()
      navigate(`/admin/skill-categories/${result.data.categoryId}`)
    } catch (error) {
      showSkillTreeError(error, t)
    } finally {
      setCreating(false)
    }
  }

  return (
    <Stack gap="md">
      {categoriesLoading ? (
        <Text c="dimmed">{t('loading')}</Text>
      ) : categories && categories.length > 0 ? (
        <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
          {categories.map((category) => {
            const challenges = (category.contents ?? []).filter((item) => item.kind === 'challenge').length
            const lessons = (category.contents ?? []).filter((item) => item.kind === 'lesson').length
            const treeCount = category.trees?.length ?? 0
            return (
              <Card
                key={category.categoryId}
                withBorder
                className={classes.panelCard}
                component={Link}
                to={`/admin/skill-categories/${category.categoryId}`}
              >
                <Group justify="space-between" wrap="nowrap">
                  <Group gap="xs" wrap="nowrap">
                    <Text size="xl" aria-hidden>
                      {skillTreeIcons[(category.iconKey ?? 'flag') as SkillTreeIconKey] ?? '🚩'}
                    </Text>
                    <Text fw={600}>{category.name}</Text>
                  </Group>
                  {treeCount === 0 && (
                    <Badge color="orange" variant="light">
                      {t('category.orphan')}
                    </Badge>
                  )}
                </Group>
                <Text size="sm" c="dimmed" lineClamp={2} mt="xs">
                  {category.summary}
                </Text>
                <Group gap="xs" mt="xs">
                  <Badge variant="outline">{t('category.treeCount', { count: treeCount })}</Badge>
                  <Badge variant="outline">{t('category.challengeCount', { count: challenges })}</Badge>
                  <Badge variant="outline">{t('category.lessonCount', { count: lessons })}</Badge>
                </Group>
              </Card>
            )
          })}
        </SimpleGrid>
      ) : (
        <div className={classes.emptyState}>
          <Text c="dimmed">{t('category.empty')}</Text>
          <Button variant="light" onClick={onOpen}>
            {t('category.createTitle')}
          </Button>
        </div>
      )}

      <Modal opened={createOpen} onClose={onClose} title={t('category.createTitle')}>
        <SkillTreeForm
          values={values}
          onChange={setValues}
          onSubmit={create}
          submitting={creating}
          nameError={nameError}
          submitLabel={t('list.create')}
        />
      </Modal>
    </Stack>
  )
}

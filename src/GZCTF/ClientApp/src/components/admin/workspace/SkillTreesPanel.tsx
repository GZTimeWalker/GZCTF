import { Badge, Button, Card, Group, Modal, SimpleGrid, Stack, Text } from '@mantine/core'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { SkillTreeForm, type SkillTreeFormValues } from '@Components/admin/skill-trees/SkillTreeForm'
import { useAdminSkillTrees, useSkillTreeAdminMutations } from '@Hooks/useSkillTreeAdmin'
import { skillTreeIcons } from '@Utils/skillTreeAdmin'
import { showSkillTreeError } from '@Utils/skillTreeAdminFeedback'
import classes from '@Components/admin/workspace/AdminWorkspace.module.css'
import type { CreatePanelProps } from '@Components/admin/workspace/types'

export const SkillTreesPanel = ({ createOpen, onClose, onOpen }: CreatePanelProps) => {
  const { t } = useTranslation('skillTrees')
  const { data: trees, isLoading: treesLoading } = useAdminSkillTrees()
  const { createTree } = useSkillTreeAdminMutations()
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
      const result = await createTree({ name, summary: values.summary.trim(), iconKey: values.iconKey })
      onClose()
      navigate(`/admin/skill-trees/${result.data.skillTreeId}`)
    } catch (error) {
      showSkillTreeError(error, t)
    } finally {
      setCreating(false)
    }
  }

  return (
    <Stack gap="md">
      {treesLoading ? (
        <Text c="dimmed">{t('loading')}</Text>
      ) : trees && trees.length > 0 ? (
        <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
          {trees.map((tree) => (
            <Card
              key={tree.skillTreeId}
              withBorder
              className={classes.panelCard}
              component={Link}
              to={`/admin/skill-trees/${tree.skillTreeId}`}
            >
              <Group justify="space-between" wrap="nowrap">
                <Group gap="xs" wrap="nowrap">
                  <Text size="xl" aria-hidden>
                    {skillTreeIcons[(tree.iconKey ?? 'flag') as keyof typeof skillTreeIcons] ?? '🚩'}
                  </Text>
                  <Text fw={600}>{tree.name}</Text>
                </Group>
                <Badge color={tree.isPublished ? 'green' : 'gray'} variant="light">
                  {tree.isPublished ? t('list.published') : t('list.draft')}
                </Badge>
              </Group>
              <Text size="sm" c="dimmed" lineClamp={2} mt="xs">
                {tree.summary}
              </Text>
            </Card>
          ))}
        </SimpleGrid>
      ) : (
        <div className={classes.emptyState}>
          <Text c="dimmed">{t('list.empty')}</Text>
          <Button variant="light" onClick={onOpen}>
            {t('list.createTitle')}
          </Button>
        </div>
      )}

      <Modal opened={createOpen} onClose={onClose} title={t('list.createTitle')}>
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

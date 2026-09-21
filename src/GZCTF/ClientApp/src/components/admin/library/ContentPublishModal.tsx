import { Alert, Badge, Button, Chip, Group, Modal, Stack, Text, TextInput } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { PresetIconPicker } from '@Components/admin/skill-trees/PresetIconPicker'
import { useAdminSkillCategories, useAdminSkillTrees, useSkillTreeAdminMutations } from '@Hooks/useSkillTreeAdmin'
import { getApiErrorCode, type SkillTreeIconKey } from '@Utils/skillTreeAdmin'
import { showSkillTreeError } from '@Utils/skillTreeAdminFeedback'

export type ContentPublishSelection = {
  categoryIds: string[]
  inlineCategories: Array<{
    skillTreeId: string
    name: string
    summary: string
    iconKey: SkillTreeIconKey
  }>
}

type ContentPublishModalProps = {
  opened: boolean
  kind: 'challenge' | 'lesson'
  contentId: string
  rowVersion: number
  initialCategoryIds: string[]
  onClose: () => void
  onPublished: () => void | Promise<void>
}

export const ContentPublishModal = ({
  opened,
  kind,
  contentId,
  rowVersion,
  initialCategoryIds,
  onClose,
  onPublished,
}: ContentPublishModalProps) => {
  const { t } = useTranslation('skillTrees')
  const { data: trees } = useAdminSkillTrees()
  const { data: categories } = useAdminSkillCategories()
  const { publishChallenge, publishLesson } = useSkillTreeAdminMutations()

  const [selectedTreeId, setSelectedTreeId] = useState<string | null>(null)
  const [selectedCategoryIds, setSelectedCategoryIds] = useState<string[]>([])
  const [inline, setInline] = useState<{ name: string; summary: string; iconKey: SkillTreeIconKey }>({
    name: '',
    summary: '',
    iconKey: 'flag',
  })
  const [error, setError] = useState<string>()
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (!opened) return
    setSelectedCategoryIds(initialCategoryIds ?? [])
    setSelectedTreeId(null)
    setInline({ name: '', summary: '', iconKey: 'flag' })
    setError(undefined)
  }, [opened, initialCategoryIds])

  const treeCategories = (categories ?? []).filter((category) =>
    category.trees?.some((reference) => reference.skillTreeId === selectedTreeId)
  )
  const selectedCategories = (categories ?? []).filter((category) =>
    selectedCategoryIds.includes(category.categoryId ?? '')
  )
  const emptyTree = Boolean(selectedTreeId) && treeCategories.length === 0

  const toggleCategory = (categoryId: string, checked: boolean) => {
    setSelectedCategoryIds((current) =>
      checked ? [...current, categoryId] : current.filter((id) => id !== categoryId)
    )
  }

  const publish = async () => {
    const inlineCategories = emptyTree && selectedTreeId && inline.name.trim()
      ? [{ skillTreeId: selectedTreeId, name: inline.name.trim(), summary: inline.summary.trim(), iconKey: inline.iconKey }]
      : []

    if (selectedCategoryIds.length === 0 && inlineCategories.length === 0) {
      setError(t('publish.categoryRequired'))
      return
    }

    setSubmitting(true)
    setError(undefined)
    try {
      const command = { rowVersion, categoryIds: selectedCategoryIds, inlineCategories }
      if (kind === 'challenge') await publishChallenge(contentId, command)
      else await publishLesson(contentId, command)
      showNotification({
        color: 'green',
        message: kind === 'challenge' ? t('publish.successChallenge') : t('publish.successLesson'),
      })
      await onPublished()
      onClose()
    } catch (publishError) {
      const code = getApiErrorCode(publishError)
      if (code === 'content_category_required') setError(t('publish.categoryRequired'))
      else if (code === 'content_category_has_no_active_tree') setError(t('publish.categoryHasNoTree'))
      else showSkillTreeError(publishError, t)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={kind === 'challenge' ? t('publish.challengeTitle') : t('publish.lessonTitle')}
      size="lg"
    >
      <Stack>
        <Stack gap={4}>
          <Text size="sm" fw={500}>
            {t('publish.selectTree')}
          </Text>
          <Group gap="xs">
            {(trees ?? []).map((tree) => (
              <Chip
                key={tree.skillTreeId}
                checked={selectedTreeId === tree.skillTreeId}
                onChange={() => setSelectedTreeId(tree.skillTreeId ?? null)}
              >
                {tree.name}
              </Chip>
            ))}
          </Group>
        </Stack>

        {selectedTreeId && (
          <Stack gap={4}>
            <Text size="sm" fw={500}>
              {t('publish.selectCategories')}
            </Text>
            {emptyTree ? (
              <Alert color="yellow">{t('publish.emptyTreeHint')}</Alert>
            ) : (
              <Group gap="xs">
                {treeCategories.map((category) => (
                  <Chip
                    key={category.categoryId}
                    checked={selectedCategoryIds.includes(category.categoryId ?? '')}
                    onChange={(checked) => toggleCategory(category.categoryId ?? '', checked)}
                  >
                    {category.name}
                  </Chip>
                ))}
              </Group>
            )}
          </Stack>
        )}

        {emptyTree && (
          <Stack>
            <TextInput
              label={t('publish.inlineName')}
              value={inline.name}
              onChange={(event) => setInline({ ...inline, name: event.currentTarget.value })}
            />
            <TextInput
              label={t('publish.inlineSummary')}
              value={inline.summary}
              onChange={(event) => setInline({ ...inline, summary: event.currentTarget.value })}
            />
            <PresetIconPicker
              value={inline.iconKey}
              onChange={(iconKey) => setInline({ ...inline, iconKey })}
            />
          </Stack>
        )}

        {selectedCategories.length > 0 && (
          <Stack gap={4}>
            <Text size="sm" fw={500}>
              {t('publish.selectedCategories')}
            </Text>
            <Group gap="xs">
              {selectedCategories.map((category) => (
                <Badge key={category.categoryId} variant="light">
                  {category.name}
                </Badge>
              ))}
            </Group>
          </Stack>
        )}

        {error && <Alert color="red">{error}</Alert>}

        <Group justify="flex-end">
          <Button variant="default" onClick={onClose} disabled={submitting}>
            {t('publish.cancel')}
          </Button>
          <Button loading={submitting} onClick={publish}>
            {t('publish.publish')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

import { Alert, Badge, Button, Divider, Group, Modal, Stack, Text, Title } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router'
import api, { Role, type SkillTreeCategoryAdminResponse, type SkillTreeDeleteImpactResponse } from '@Api'
import { SkillTreeForm, type SkillTreeFormValues } from '@Components/admin/skill-trees/SkillTreeForm'
import { CategoryPicker, type NewCategoryValues } from '@Components/admin/skill-trees/CategoryPicker'
import { SortableCategoryList, type DraftCategory } from '@Components/admin/skill-trees/SortableCategoryList'
import { TypedDeleteModal } from '@Components/admin/shared/TypedDeleteModal'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useAdminSkillCategories, useAdminSkillTreeDraft, useSkillTreeAdminMutations } from '@Hooks/useSkillTreeAdmin'
import { moveItem, skillTreeIcons, type SkillTreeIconKey } from '@Utils/skillTreeAdmin'
import { showSkillTreeError } from '@Utils/skillTreeAdminFeedback'
import { getApiErrorCode } from '@Utils/skillTreeAdmin'

const toDraftCategory = (category: SkillTreeCategoryAdminResponse): DraftCategory => ({
  categoryId: category.categoryId ?? '',
  name: category.name ?? '',
  summary: category.summary ?? '',
  iconKey: (category.iconKey ?? 'flag') as SkillTreeIconKey,
  rowVersion: category.rowVersion ?? 0,
  sortOrder: category.sortOrder ?? 0,
})

const AdminSkillTreeEdit = () => {
  const { id } = useParams()
  const { t } = useTranslation('skillTrees')
  const { data: draft, mutate: mutateDraft } = useAdminSkillTreeDraft(id)
  const { data: allCategories, mutate: mutateCategories } = useAdminSkillCategories()
  const { createCategory, saveTreeDraft, publishTree, deleteTree } = useSkillTreeAdminMutations()
  const navigate = useNavigate()

  const initializedRevision = useRef<string | undefined>(undefined)
  const [values, setValues] = useState<SkillTreeFormValues>({ name: '', summary: '', iconKey: 'flag' })
  const [categories, setCategories] = useState<DraftCategory[]>([])
  const [rowVersion, setRowVersion] = useState<number>()
  const [dirty, setDirty] = useState(false)
  const [conflict, setConflict] = useState(false)
  const [saving, setSaving] = useState(false)
  const [creating, setCreating] = useState(false)
  const [previewOpen, setPreviewOpen] = useState(false)
  const [preview, setPreview] = useState<SkillTreeCategoryAdminResponse[] | undefined>()
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [deleteLoading, setDeleteLoading] = useState(false)
  const [impact, setImpact] = useState<SkillTreeDeleteImpactResponse>()

  useEffect(() => {
    if (!draft?.revisionId || initializedRevision.current === draft.revisionId) return
    initializedRevision.current = draft.revisionId
    setValues({
      name: draft.name ?? '',
      summary: draft.summary ?? '',
      iconKey: (draft.iconKey ?? 'flag') as SkillTreeIconKey,
    })
    setCategories((draft.categories ?? []).map(toDraftCategory))
    setRowVersion(draft.rowVersion)
    setDirty(false)
    setConflict(false)
  }, [draft])

  if (!id) return null

  const saveDraft = async () => {
    const result = await saveTreeDraft(id, {
      name: values.name,
      summary: values.summary,
      iconKey: values.iconKey,
      rowVersion,
      categories: categories.map((category, sortOrder) => ({ categoryId: category.categoryId, sortOrder })),
    })
    setRowVersion(result.data.rowVersion)
    setDirty(false)
    await mutateDraft()
    return result.data.rowVersion
  }

  const save = async () => {
    setSaving(true)
    try {
      await saveDraft()
      showNotification({ color: 'green', message: t('editor.saveSuccess') })
    } catch (error) {
      if (getApiErrorCode(error) === 'skill_tree_revision_conflict') setConflict(true)
      showSkillTreeError(error, t)
    } finally {
      setSaving(false)
    }
  }

  const publish = async () => {
    setSaving(true)
    try {
      let version = rowVersion
      if (dirty) version = await saveDraft()
      await publishTree(id, { rowVersion: version })
      initializedRevision.current = undefined
      await mutateDraft()
      showNotification({ color: 'green', message: t('editor.publishSuccess') })
    } catch (error) {
      if (getApiErrorCode(error) === 'skill_tree_revision_conflict') setConflict(true)
      showSkillTreeError(error, t)
    } finally {
      setSaving(false)
    }
  }

  const reload = async () => {
    initializedRevision.current = undefined
    setConflict(false)
    await mutateDraft()
    await mutateCategories()
  }

  const openPreview = async () => {
    try {
      const response = await api.adminSkillTrees.adminSkillTreesPreviewDraft(id)
      setPreview(response.data.categories ?? [])
      setPreviewOpen(true)
    } catch (error) {
      showSkillTreeError(error, t)
    }
  }

  const addCategory = (category: SkillTreeCategoryAdminResponse) => {
    setCategories((current) =>
      current.some((item) => item.categoryId === category.categoryId)
        ? current
        : [...current, { ...toDraftCategory(category), sortOrder: current.length }])
    setDirty(true)
  }

  const createCategoryInline = async (newCategory: NewCategoryValues) => {
    setCreating(true)
    try {
      const result = await createCategory({
        name: newCategory.name,
        summary: newCategory.summary,
        iconKey: newCategory.iconKey,
        rowVersion: undefined,
      })
      addCategory(result.data as SkillTreeCategoryAdminResponse)
      await mutateCategories()
    } catch (error) {
      showSkillTreeError(error, t)
    } finally {
      setCreating(false)
    }
  }

  const removeCategory = (categoryId: string) => {
    setCategories((current) => current.filter((item) => item.categoryId !== categoryId))
    setDirty(true)
  }

  const moveCategory = (from: number, to: number) => {
    setCategories((current) => moveItem(current, from, to))
    setDirty(true)
  }

  const openDelete = async () => {
    setDeleteLoading(true)
    try {
      const response = await api.adminSkillTrees.adminSkillTreesGetDeleteImpact(id)
      setImpact(response.data)
      setDeleteOpen(true)
    } catch (error) {
      showSkillTreeError(error, t)
    } finally {
      setDeleteLoading(false)
    }
  }

  const confirmDelete = async (confirmationName: string) => {
    setSaving(true)
    try {
      await deleteTree(id, { confirmationName, rowVersion: impact?.rowVersion ?? 0 })
      setDeleteOpen(false)
      navigate('/admin/skill-trees')
    } catch (error) {
      handleDeleteError(error)
    } finally {
      setSaving(false)
    }
  }

  const handleDeleteError = (error: unknown) => {
    if (getApiErrorCode(error) === 'skill_tree_revision_conflict') setConflict(true)
    showSkillTreeError(error, t)
  }

  const impactLines = impact
    ? [
        t('delete.categoryCount', { count: impact.categoryCount ?? 0 }),
        t('delete.challengeCount', { count: impact.challengeCount ?? 0 }),
        t('delete.lessonCount', { count: impact.lessonCount ?? 0 }),
        t('delete.enrollmentCount', { count: impact.enrollmentCount ?? 0 }),
      ]
    : []

  return (
    <WithRole requiredRole={Role.Admin}>
      <WithNavBar minWidth={0}>
        <Stack>
          <Group justify="space-between">
            <Title order={1}>{t('editor.title')}</Title>
            <Group>
              <Badge color={draft?.revisionId ? 'gray' : 'green'} variant="light">
                {t('editor.draft')}
              </Badge>
              <Button component={Link} to="/admin/skill-trees" variant="subtle">
                {t('list.title')}
              </Button>
            </Group>
          </Group>

          {conflict && (
            <Alert color="red" title={t('errors.revisionConflict')}>
              <Group>
                <Text size="sm">{t('editor.revisionConflict')}</Text>
                <Button size="compact-sm" onClick={reload}>
                  {t('editor.reload')}
                </Button>
              </Group>
            </Alert>
          )}

          {!draft ? (
            <Text c="dimmed">{t('loading')}</Text>
          ) : (
            <>
              <SkillTreeForm
                values={values}
                onChange={(next) => {
                  setValues(next)
                  setDirty(true)
                }}
                onSubmit={save}
                submitting={saving}
                submitLabel={t('editor.save')}
              />

              <Divider label={t('editor.categories')} labelPosition="left" mt="md" />

              <SortableCategoryList
                categories={categories}
                onMove={moveCategory}
                onRemove={removeCategory}
                disabled={saving}
              />

              <CategoryPicker
                categories={allCategories ?? []}
                selectedIds={categories.map((category) => category.categoryId)}
                onSelect={addCategory}
                onCreate={createCategoryInline}
                creating={creating}
                disabled={saving}
              />

              <Group mt="md">
                <Button variant="light" disabled={saving} onClick={openPreview}>
                  {t('editor.preview')}
                </Button>
                <Button variant="filled" color="green" loading={saving} onClick={publish}>
                  {t('editor.publish')}
                </Button>
                <Button color="red" variant="light" loading={deleteLoading} onClick={openDelete}>
                  {t('delete.deleteTree')}
                </Button>
              </Group>
            </>
          )}
        </Stack>

        <Modal opened={previewOpen} onClose={() => setPreviewOpen(false)} title={t('editor.previewTitle')} size="lg">
          <Stack>
            {!preview || preview.length === 0 ? (
              <Text c="dimmed">{t('editor.emptyPreview')}</Text>
            ) : (
              preview.map((category) => (
                <Stack key={category.categoryId} gap={4}>
                  <Group gap="xs">
                    <Text aria-hidden>{skillTreeIcons[(category.iconKey ?? 'flag') as SkillTreeIconKey] ?? '🚩'}</Text>
                    <Text fw={600}>{category.name}</Text>
                  </Group>
                  {category.contents && category.contents.length > 0 ? (
                    category.contents
                      .slice()
                      .sort((left, right) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0))
                      .map((content) => (
                        <Text key={`${category.categoryId}-${content.contentId}`} size="sm" pl="lg">
                          {content.title}
                        </Text>
                      ))
                  ) : (
                    <Text size="sm" c="dimmed" pl="lg">
                      {t('editor.emptyPreview')}
                    </Text>
                  )}
                </Stack>
              ))
            )}
          </Stack>
        </Modal>

        <TypedDeleteModal
          opened={deleteOpen}
          entityName={values.name}
          title={t('delete.deleteTree')}
          impactLines={impactLines}
          requiresTypedConfirmation={Boolean(impact?.requiresTypedConfirmation)}
          loading={saving}
          onClose={() => setDeleteOpen(false)}
          onConfirm={confirmDelete}
        />
      </WithNavBar>
    </WithRole>
  )
}

export default AdminSkillTreeEdit

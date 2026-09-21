import { Alert, Badge, Button, Checkbox, Divider, Group, Stack, Text, Title } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router'
import api, { Role, type CategoryDeleteImpactResponse, type SkillCategoryAdminResponse, type SkillTreeContentSummaryResponse } from '@Api'
import { SkillTreeForm, type SkillTreeFormValues } from '@Components/admin/skill-trees/SkillTreeForm'
import { CategoryContentList } from '@Components/admin/skill-categories/CategoryContentList'
import { CategoryMergeModal } from '@Components/admin/skill-categories/CategoryMergeModal'
import { TypedDeleteModal } from '@Components/admin/shared/TypedDeleteModal'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useAdminSkillCategories, useAdminSkillCategory, useAdminSkillTrees, useSkillTreeAdminMutations } from '@Hooks/useSkillTreeAdmin'
import { getApiErrorCode, moveItem, type SkillTreeIconKey } from '@Utils/skillTreeAdmin'
import { showSkillTreeError } from '@Utils/skillTreeAdminFeedback'

const AdminSkillCategoryEdit = () => {
  const { id } = useParams()
  const { t } = useTranslation('skillTrees')
  const { data: category, mutate } = useAdminSkillCategory(id)
  const { data: trees } = useAdminSkillTrees()
  const { data: allCategories } = useAdminSkillCategories()
  const {
    saveCategory,
    sortCategoryContents,
    saveTreeMemberships,
    mergeCategories,
    deleteCategory,
  } = useSkillTreeAdminMutations()
  const navigate = useNavigate()

  const initialized = useRef<string | undefined>(undefined)
  const [values, setValues] = useState<SkillTreeFormValues>({ name: '', summary: '', iconKey: 'flag' })
  const [contents, setContents] = useState<SkillTreeContentSummaryResponse[]>([])
  const [memberships, setMemberships] = useState<Record<string, boolean>>({})
  const [affectedTreeIds, setAffectedTreeIds] = useState<string[]>([])
  const [conflict, setConflict] = useState(false)
  const [saving, setSaving] = useState(false)
  const [mergeOpen, setMergeOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [deleteLoading, setDeleteLoading] = useState(false)
  const [impact, setImpact] = useState<CategoryDeleteImpactResponse>()

  useEffect(() => {
    if (!category?.categoryId || initialized.current === category.categoryId) return
    initialized.current = category.categoryId
    setValues({
      name: category.name ?? '',
      summary: category.summary ?? '',
      iconKey: (category.iconKey ?? 'flag') as SkillTreeIconKey,
    })
    setContents((category.contents ?? []).slice().sort((left, right) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0)))
    const state: Record<string, boolean> = {}
    for (const tree of trees ?? []) state[tree.skillTreeId ?? ''] = false
    for (const reference of category.trees ?? []) state[reference.skillTreeId ?? ''] = true
    setMemberships(state)
    setConflict(false)
  }, [category, trees])

  if (!id) return null

  const handleError = (error: unknown) => {
    if (getApiErrorCode(error) === 'skill_tree_revision_conflict') setConflict(true)
    showSkillTreeError(error, t)
  }

  const saveMetadata = async () => {
    setSaving(true)
    try {
      await saveCategory(id, {
        name: values.name,
        summary: values.summary,
        iconKey: values.iconKey,
        rowVersion: category?.rowVersion,
      })
      await mutate()
      showNotification({ color: 'green', message: t('category.metadataSuccess') })
    } catch (error) {
      handleError(error)
    } finally {
      setSaving(false)
    }
  }

  const saveContents = async () => {
    setSaving(true)
    try {
      await sortCategoryContents(id, {
        rowVersion: category?.rowVersion ?? 0,
        contents: contents.map((content, sortOrder) => ({
          kind: content.kind ?? '',
          contentId: content.contentId ?? '',
          sortOrder,
        })),
      })
      await mutate()
      showNotification({ color: 'green', message: t('category.orderSuccess') })
    } catch (error) {
      handleError(error)
    } finally {
      setSaving(false)
    }
  }

  const saveMembership = async () => {
    setSaving(true)
    try {
      const response = await saveTreeMemberships(id, {
        categoryRowVersion: category?.rowVersion ?? 0,
        trees: (trees ?? []).map((tree) => ({
          skillTreeId: tree.skillTreeId ?? '',
          included: Boolean(memberships[tree.skillTreeId ?? '']),
          skillTreeRowVersion: tree.rowVersion ?? 0,
        })),
      })
      setAffectedTreeIds(response.data.affectedSkillTreeIds ?? [])
      await mutate()
      showNotification({ color: 'green', message: t('category.membershipSuccess') })
    } catch (error) {
      handleError(error)
    } finally {
      setSaving(false)
    }
  }

  const submitMerge = async (duplicate: SkillCategoryAdminResponse) => {
    setSaving(true)
    try {
      await mergeCategories({
        survivorCategoryId: id,
        duplicateCategoryId: duplicate.categoryId ?? '',
        survivorRowVersion: category?.rowVersion ?? 0,
        duplicateRowVersion: duplicate.rowVersion ?? 0,
      })
      setMergeOpen(false)
      showNotification({ color: 'green', message: t('merge.success') })
      navigate('/admin/skill-categories')
    } catch (error) {
      handleError(error)
    } finally {
      setSaving(false)
    }
  }

  const openDelete = async () => {
    setDeleteLoading(true)
    try {
      const response = await api.adminSkillCategories.adminSkillCategoriesGetDeleteImpact(id)
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
      await deleteCategory(id, { confirmationName, rowVersion: category?.rowVersion ?? 0 })
      setDeleteOpen(false)
      showNotification({ color: 'green', message: t('delete.success') })
      navigate('/admin/skill-categories')
    } catch (error) {
      handleError(error)
    } finally {
      setSaving(false)
    }
  }

  const impactLines = impact
    ? [
        t('delete.draftTreeCount', { count: impact.draftTreeCount ?? 0 }),
        t('delete.publishedTreeCount', { count: impact.publishedTreeCount ?? 0 }),
        t('delete.challengeCount', { count: impact.challengeCount ?? 0 }),
        t('delete.lessonCount', { count: impact.lessonCount ?? 0 }),
      ]
    : []

  return (
    <WithRole requiredRole={Role.Admin}>
      <WithNavBar minWidth={0}>
        <Stack>
          <Group justify="space-between">
            <Title order={1}>{category?.name ?? t('category.title')}</Title>
            <Button component={Link} to="/admin/skill-categories" variant="subtle">
              {t('category.title')}
            </Button>
          </Group>

          {conflict && (
            <Alert color="red" title={t('errors.revisionConflict')}>
              <Button size="compact-sm" onClick={() => { setConflict(false); void mutate() }}>
                {t('editor.reload')}
              </Button>
            </Alert>
          )}

          {!category ? (
            <Text c="dimmed">{t('loading')}</Text>
          ) : (
            <>
              <Divider label={t('category.metadata')} labelPosition="left" />
              <SkillTreeForm
                values={values}
                onChange={setValues}
                onSubmit={saveMetadata}
                submitting={saving}
                submitLabel={t('category.saveMetadata')}
              />

              <Divider label={t('category.memberships')} labelPosition="left" mt="md" />
              <Text size="sm" c="dimmed">
                {t('category.membershipHint')}
              </Text>
              <Stack gap="xs">
                {(trees ?? []).map((tree) => (
                  <Checkbox
                    key={tree.skillTreeId}
                    checked={Boolean(memberships[tree.skillTreeId ?? ''])}
                    label={
                      <Group gap="xs">
                        <Text>{tree.name}</Text>
                        <Badge color={tree.isPublished ? 'green' : 'gray'} variant="light">
                          {tree.isPublished ? t('category.published') : t('category.draft')}
                        </Badge>
                      </Group>
                    }
                    onChange={(event) =>
                      setMemberships((current) => ({
                        ...current,
                        [tree.skillTreeId ?? '']: event.currentTarget.checked,
                      }))
                    }
                  />
                ))}
              </Stack>
              <Button w="fit-content" loading={saving} onClick={saveMembership}>
                {t('category.saveMetadata')}
              </Button>
              {affectedTreeIds.length > 0 && (
                <Alert color="yellow" title={t('category.hasUnpublished')}>
                  <Group gap="xs">
                    {affectedTreeIds.map((treeId) => (
                      <Button
                        key={treeId}
                        size="compact-sm"
                        variant="light"
                        component={Link}
                        to={`/admin/skill-trees/${treeId}`}
                      >
                        {t('category.previewLink')}
                      </Button>
                    ))}
                  </Group>
                </Alert>
              )}

              <Divider label={t('category.contents')} labelPosition="left" mt="md" />
              <Text size="sm" c="dimmed">
                {t('category.contentHint')}
              </Text>
              <CategoryContentList
                contents={contents}
                disabled={saving}
                onMove={(from, to) => setContents((current) => moveItem(current, from, to))}
              />
              <Button w="fit-content" loading={saving} onClick={saveContents}>
                {t('category.saveOrder')}
              </Button>

              <Group mt="xl">
                <Button variant="light" onClick={() => setMergeOpen(true)}>
                  {t('merge.title')}
                </Button>
                <Button color="red" variant="light" loading={deleteLoading} onClick={openDelete}>
                  {t('delete.deleteCategory')}
                </Button>
              </Group>
            </>
          )}
        </Stack>

        <CategoryMergeModal
          opened={mergeOpen}
          survivorId={id}
          survivorName={category?.name ?? ''}
          categories={allCategories ?? []}
          loading={saving}
          onClose={() => setMergeOpen(false)}
          onSubmit={submitMerge}
        />

        <TypedDeleteModal
          opened={deleteOpen}
          entityName={category?.name ?? ''}
          title={t('delete.deleteCategory')}
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

export default AdminSkillCategoryEdit

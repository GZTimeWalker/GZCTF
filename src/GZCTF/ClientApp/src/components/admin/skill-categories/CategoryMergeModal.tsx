import { Alert, Button, Group, Modal, Select, Stack, Text } from '@mantine/core'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { SkillCategoryAdminResponse } from '@Api'

type CategoryMergeModalProps = {
  opened: boolean
  survivorName: string
  survivorId: string
  categories: SkillCategoryAdminResponse[]
  loading: boolean
  onClose: () => void
  onSubmit: (duplicate: SkillCategoryAdminResponse) => Promise<void>
}

export const CategoryMergeModal = ({
  opened,
  survivorName,
  survivorId,
  categories,
  loading,
  onClose,
  onSubmit,
}: CategoryMergeModalProps) => {
  const { t } = useTranslation('skillTrees')
  const [duplicateId, setDuplicateId] = useState<string | null>(null)

  useEffect(() => {
    if (!opened) setDuplicateId(null)
  }, [opened])

  const candidates = categories.filter((category) => category.categoryId !== survivorId)
  const duplicate = candidates.find((category) => category.categoryId === duplicateId)

  return (
    <Modal opened={opened} onClose={onClose} title={t('merge.title')}>
      <Stack>
        <Text size="sm">
          {t('merge.survivor')}: <strong>{survivorName}</strong>
        </Text>
        <Select
          label={t('merge.duplicate')}
          value={duplicateId}
          data={candidates.map((category) => ({ value: category.categoryId ?? '', label: category.name ?? '' }))}
          onChange={setDuplicateId}
          searchable
        />
        <Alert color="blue">{t('merge.rule')}</Alert>
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose} disabled={loading}>
            {t('delete.cancel')}
          </Button>
          <Button
            loading={loading}
            disabled={!duplicate}
            onClick={() => duplicate && onSubmit(duplicate)}
          >
            {t('merge.submit')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

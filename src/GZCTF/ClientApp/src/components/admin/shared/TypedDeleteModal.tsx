import { Button, Group, Modal, Stack, Text, TextInput } from '@mantine/core'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

type TypedDeleteModalProps = {
  opened: boolean
  entityName: string
  title: string
  impactLines: string[]
  requiresTypedConfirmation: boolean
  loading: boolean
  onClose: () => void
  onConfirm: (confirmationName: string) => Promise<void>
}

export const TypedDeleteModal = ({
  opened,
  entityName,
  title,
  impactLines,
  requiresTypedConfirmation,
  loading,
  onClose,
  onConfirm,
}: TypedDeleteModalProps) => {
  const { t } = useTranslation('skillTrees')
  const [confirmation, setConfirmation] = useState('')

  useEffect(() => {
    if (!opened) setConfirmation('')
  }, [opened])

  const confirmed = !requiresTypedConfirmation || confirmation === entityName

  return (
    <Modal opened={opened} onClose={onClose} title={title}>
      <Stack>
        <Text fw={600}>{t('delete.impactTitle')}</Text>
        {impactLines.map((line) => (
          <Text key={line} size="sm">
            {line}
          </Text>
        ))}
        {requiresTypedConfirmation && (
          <TextInput
            label={t('delete.confirmLabel')}
            description={t('delete.typedHint', { name: entityName })}
            value={confirmation}
            onChange={(event) => setConfirmation(event.currentTarget.value)}
          />
        )}
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose} disabled={loading}>
            {t('delete.cancel')}
          </Button>
          <Button
            color="red"
            loading={loading}
            disabled={!confirmed}
            onClick={() => onConfirm(confirmation)}
          >
            {t('delete.confirm')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

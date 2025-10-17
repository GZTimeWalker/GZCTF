import { ModalProps, Modal, Stack, Select, Button } from '@mantine/core'
import { FC, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Division, ParticipationEditModel } from '@Api'

interface ParticipationDivisionEditModalProps extends ModalProps {
  participateId: number
  divisions: Division[]
  currentDivisionId?: number | null
  setParticipation: (id: number, model: ParticipationEditModel) => Promise<void>
}

export const ParticipationDivisionEditModal: FC<ParticipationDivisionEditModalProps> = (props) => {
  const { participateId, divisions, currentDivisionId, setParticipation, ...modalProps } = props
  const [divisionId, setDivisionId] = useState(currentDivisionId ? currentDivisionId.toString() : '')
  const [disabled, setDisabled] = useState(false)

  const { t } = useTranslation()

  const options = useMemo(() => divisions.map((div) => ({ value: div.id.toString(), label: div.name })), [divisions])

  useEffect(() => {
    setDivisionId(currentDivisionId ? currentDivisionId.toString() : '')
    setDisabled(false)
  }, [currentDivisionId, modalProps.opened])

  const onConfirm = async () => {
    const nextDivisionId = divisionId ? parseInt(divisionId, 10) : null

    if ((currentDivisionId ?? null) === nextDivisionId) {
      modalProps.onClose()
      return
    }

    setDisabled(true)

    try {
      await setParticipation(participateId, { divisionId: nextDivisionId })
      modalProps.onClose()
    } finally {
      setDisabled(false)
    }
  }

  return (
    <Modal {...modalProps}>
      <Stack>
        <Select
          label={t('game.content.join.division.label')}
          data={options}
          clearable
          disabled={disabled}
          value={divisionId}
          placeholder={t('admin.content.games.review.participation.no_division')}
          onChange={(e) => setDivisionId(e ?? '')}
        />
        <Button fullWidth disabled={disabled} onClick={onConfirm}>
          {t('common.modal.confirm_update')}
        </Button>
      </Stack>
    </Modal>
  )
}

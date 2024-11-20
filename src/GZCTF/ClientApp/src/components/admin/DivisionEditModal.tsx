import { ModalProps, Modal, Stack, Select, Button } from '@mantine/core'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ParticipationEditModel } from '@Api'

interface DivisionEditModalProps extends ModalProps {
  participateId: number
  divisions: string[]
  currentDivision: string
  setParticipation: (id: number, model: ParticipationEditModel) => Promise<void>
}

export const DivisionEditModal: FC<DivisionEditModalProps> = (props) => {
  const { participateId, divisions, currentDivision, setParticipation, ...modalProps } = props
  const [division, setDivision] = useState(currentDivision)
  const [disabled, setDisabled] = useState(false)

  const { t } = useTranslation()

  const onConfirm = () => {
    if (!division) return
    setDisabled(true)
    setParticipation(participateId, { division }).then(() => {
      setDisabled(false)
      modalProps.onClose()
    })
  }

  return (
    <Modal {...modalProps}>
      <Stack>
        <Select
          required
          label={t('game.content.join.division.label')}
          data={divisions}
          disabled={disabled}
          value={division}
          onChange={(e) => setDivision(e ?? '')}
        />
        <Button fullWidth disabled={disabled} onClick={onConfirm}>
          {t('common.modal.confirm_update')}
        </Button>
      </Stack>
    </Modal>
  )
}

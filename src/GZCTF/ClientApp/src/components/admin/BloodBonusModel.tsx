import { Button, Group, Modal, ModalProps, NumberInput, Stack, Text } from '@mantine/core'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { BloodBonus } from '@Utils/Shared'
import { OnceSWRConfig } from '@Hooks/useConfig'
import { useSyncOnChange } from '@Hooks/useSyncOnChange'
import api, { SubmissionType } from '@Api'

const toNumber = (value: string | number) => {
  if (typeof value === 'string') {
    const val = Number(value)
    return isNaN(val) ? 0 : val
  }
  return value
}

export const BloodBonusModel: FC<ModalProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { data: gameSource, mutate } = api.edit.useEditGetGame(numId, OnceSWRConfig)
  const [disabled, setDisabled] = useState(false)
  // seeded from an already cached `gameSource`, kept in sync below
  const initialBonus = (type: SubmissionType) =>
    gameSource ? new BloodBonus(gameSource.bloodBonus).getBonusNum(type) : 0

  const [firstBloodBonus, setFirstBloodBonus] = useState(() => initialBonus(SubmissionType.FirstBlood))
  const [secondBloodBonus, setSecondBloodBonus] = useState(() => initialBonus(SubmissionType.SecondBlood))
  const [thirdBloodBonus, setThirdBloodBonus] = useState(() => initialBonus(SubmissionType.ThirdBlood))

  const { t } = useTranslation()

  useSyncOnChange([gameSource], () => {
    if (gameSource) {
      const bonus = new BloodBonus(gameSource.bloodBonus)
      setFirstBloodBonus(bonus.getBonusNum(SubmissionType.FirstBlood))
      setSecondBloodBonus(bonus.getBonusNum(SubmissionType.SecondBlood))
      setThirdBloodBonus(bonus.getBonusNum(SubmissionType.ThirdBlood))
    }
  })

  const onUpdate = async () => {
    if (!gameSource?.title) return
    setDisabled(true)

    try {
      await api.edit.editUpdateGame(numId, {
        ...gameSource,
        bloodBonus: BloodBonus.fromBonus(firstBloodBonus, secondBloodBonus, thirdBloodBonus).value,
      })
      mutate()
      props.onClose()
    } finally {
      setDisabled(false)
    }
  }

  return (
    <Modal {...props}>
      <Stack>
        <Text>{t('admin.content.games.challenges.bonus.description')}</Text>
        <NumberInput
          label={t('admin.content.games.challenges.bonus.first_blood')}
          defaultValue={5}
          decimalScale={1}
          fixedDecimalScale
          min={0}
          step={1}
          max={100}
          disabled={disabled}
          value={firstBloodBonus / 10}
          onChange={(value) => setFirstBloodBonus(Math.floor(toNumber(value) * 10))}
        />
        <NumberInput
          label={t('admin.content.games.challenges.bonus.second_blood')}
          defaultValue={3}
          decimalScale={1}
          fixedDecimalScale
          min={0}
          step={1}
          max={100}
          disabled={disabled}
          value={secondBloodBonus / 10}
          onChange={(value) => setSecondBloodBonus(Math.floor(toNumber(value) * 10))}
        />
        <NumberInput
          label={t('admin.content.games.challenges.bonus.third_blood')}
          defaultValue={1}
          decimalScale={1}
          fixedDecimalScale
          min={0}
          step={1}
          max={100}
          disabled={disabled}
          value={thirdBloodBonus / 10}
          onChange={(value) => setThirdBloodBonus(Math.floor(toNumber(value) * 10))}
        />
        <Group grow m="auto" w="100%">
          <Button fullWidth disabled={disabled} onClick={onUpdate}>
            {t('admin.button.save')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

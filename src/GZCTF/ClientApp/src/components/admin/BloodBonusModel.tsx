import { Button, Group, Modal, ModalProps, NumberInput, Stack, Text } from '@mantine/core'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import { BloodBonus } from '@Utils/Shared'
import { OnceSWRConfig } from '@Utils/useConfig'
import api, { SubmissionType } from '@Api'

const BloodBonusModel: FC<ModalProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { data: gameSource, mutate } = api.edit.useEditGetGame(numId, OnceSWRConfig)
  const [disabled, setDisabled] = useState(false)
  const [firstBloodBonus, setFirstBloodBonus] = useState(0)
  const [secondBloodBonus, setSecondBloodBonus] = useState(0)
  const [thirdBloodBonus, setThirdBloodBonus] = useState(0)

  const { t } = useTranslation()

  useEffect(() => {
    if (gameSource) {
      const bonus = new BloodBonus(gameSource.bloodBonus)
      setFirstBloodBonus(bonus.getBonusNum(SubmissionType.FirstBlood))
      setSecondBloodBonus(bonus.getBonusNum(SubmissionType.SecondBlood))
      setThirdBloodBonus(bonus.getBonusNum(SubmissionType.ThirdBlood))
    }
  }, [gameSource])

  const onUpdate = () => {
    if (!gameSource?.title) return

    setDisabled(true)
    api.edit
      .editUpdateGame(numId, {
        ...gameSource,
        bloodBonus: BloodBonus.fromBonus(firstBloodBonus, secondBloodBonus, thirdBloodBonus).value,
      })
      .then(() => {
        mutate()
        props.onClose()
      })
      .finally(() => {
        setDisabled(false)
      })
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
          onChange={(value) =>
            typeof value === 'number' && setFirstBloodBonus(Math.floor(value * 10))
          }
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
          onChange={(value) =>
            typeof value === 'number' && setSecondBloodBonus(Math.floor(value * 10))
          }
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
          onChange={(value) =>
            typeof value === 'number' && setThirdBloodBonus(Math.floor(value * 10))
          }
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

export default BloodBonusModel

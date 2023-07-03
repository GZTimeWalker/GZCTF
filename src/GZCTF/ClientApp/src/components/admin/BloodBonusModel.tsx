import { FC, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Button, Group, Modal, ModalProps, NumberInput, Stack, Text } from '@mantine/core'
import { BloodBonus } from '@Utils/Shared'
import api, { SubmissionType } from '@Api'

const BloodBonusModel: FC<ModalProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { data: gameSource, mutate } = api.edit.useEditGetGame(numId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })
  const [disabled, setDisabled] = useState(false)
  const [firstBloodBonus, setFirstBloodBonus] = useState(0)
  const [secondBloodBonus, setSecondBloodBonus] = useState(0)
  const [thirdBloodBonus, setThirdBloodBonus] = useState(0)

  useEffect(() => {
    if (gameSource) {
      const bonus = new BloodBonus(gameSource.bloodBonus)
      setFirstBloodBonus(bonus.getBonusNum(SubmissionType.FirstBlood))
      setSecondBloodBonus(bonus.getBonusNum(SubmissionType.SecondBlood))
      setThirdBloodBonus(bonus.getBonusNum(SubmissionType.ThirdBlood))
    }
  }, [gameSource])

  const onUpdate = () => {
    if (gameSource && gameSource.title) {
      setDisabled(true)
      api.edit
        .editUpdateGame(numId, {
          ...gameSource,
          bloodBonus: BloodBonus.fromBonus(firstBloodBonus, secondBloodBonus, thirdBloodBonus)
            .value,
        })
        .then(() => {
          mutate()
          props.onClose()
        })
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  return (
    <Modal {...props}>
      <Stack>
        <Text>
          三血奖励加成是指当一个题目被前三个队伍解出时，每个队伍可以得到的分值奖励。三血的奖励基于题目的当前分值，并以一个固定百分比的形式累加至该队伍的得分中。
        </Text>
        <NumberInput
          label="一血奖励 (%)"
          defaultValue={5}
          precision={1}
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
          label="二血奖励 (%)"
          defaultValue={3}
          precision={1}
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
          label="三血奖励 (%)"
          defaultValue={1}
          precision={1}
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
            保存更改
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default BloodBonusModel

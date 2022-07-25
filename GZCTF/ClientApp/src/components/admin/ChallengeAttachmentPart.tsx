import React, { FC } from 'react'
import { SimpleGrid, TextInput, Stack, NumberInput, Accordion } from '@mantine/core'
import { ChallengeType } from '../../Api'
import { ChallengeEditPartProps, ChallengeAccordionControl } from '../ChallengeItem'

const ChallengeAttachmentPart: FC<ChallengeEditPartProps> = (props) => {
  const { challengeInfo, setChallengeInfo, disabled, value, curValue, onUpdate } = props

  return (
    <Accordion.Item value={value}>
      <ChallengeAccordionControl
        btnDisabled={disabled}
        shown={curValue === value}
        onBtnClick={() => onUpdate(challengeInfo)}
      >
        题目附件及 flag 设置
      </ChallengeAccordionControl>
      <Accordion.Panel>
        <Stack>

        </Stack>
      </Accordion.Panel>
    </Accordion.Item>
  )
}

export default ChallengeAttachmentPart

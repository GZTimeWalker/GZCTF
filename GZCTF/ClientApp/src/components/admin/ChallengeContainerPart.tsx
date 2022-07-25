import React, { FC } from 'react'
import { SimpleGrid, TextInput, Stack, NumberInput, Accordion } from '@mantine/core'
import { ChallengeType } from '../../Api'
import { ChallengeEditPartProps, ChallengeAccordionControl } from '../ChallengeItem'

const ChallengeContainerPart: FC<ChallengeEditPartProps> = (props) => {
  const { challengeInfo, setChallengeInfo, disabled, value, curValue, onUpdate } = props

  return (
    <Accordion.Item value={value}>
      <ChallengeAccordionControl
        btnDisabled={disabled}
        shown={curValue === value}
        disabled={
          challengeInfo.type === ChallengeType.StaticAttachment ||
          challengeInfo.type === ChallengeType.DynamicAttachment
        }
        onBtnClick={() => onUpdate(challengeInfo)}
      >
        容器信息
      </ChallengeAccordionControl>
      <Accordion.Panel>
        <Stack>
          <TextInput
            label="镜像链接"
            disabled={disabled}
            value={challengeInfo.containerImage ?? ''}
            required
            onChange={(e) => setChallengeInfo({ ...challengeInfo, containerImage: e.target.value })}
          />
          <SimpleGrid cols={3}>
            <NumberInput
              label="服务端口"
              min={1}
              max={65535}
              required
              disabled={disabled}
              stepHoldDelay={500}
              stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
              value={challengeInfo.containerExposePort ?? 1}
              onChange={(e) => setChallengeInfo({ ...challengeInfo, containerExposePort: e })}
            />
            <NumberInput
              label="CPU 数量限制"
              min={1}
              max={16}
              required
              disabled={disabled}
              stepHoldDelay={500}
              stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
              value={challengeInfo.cpuCount ?? 1}
              onChange={(e) => setChallengeInfo({ ...challengeInfo, cpuCount: e })}
            />
            <NumberInput
              label="内存限制 (MB)"
              min={64}
              max={8192}
              required
              disabled={disabled}
              stepHoldDelay={500}
              stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
              value={challengeInfo.memoryLimit ?? 1}
              onChange={(e) => setChallengeInfo({ ...challengeInfo, memoryLimit: e })}
            />
          </SimpleGrid>
        </Stack>
      </Accordion.Panel>
    </Accordion.Item>
  )
}

export default ChallengeContainerPart

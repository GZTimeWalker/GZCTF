import dayjs from 'dayjs'
import { FC } from 'react'
import { Stack, Text } from '@mantine/core'
import { SelectableItemProps, SelectableItem } from '@Components/ScrollSelect'
import { HunamizeSize } from '@Utils/Shared'
import { ChallengeTrafficModel, TeamTrafficModel, FileRecord } from '@Api'

export const ChallengeItem: FC<SelectableItemProps<ChallengeTrafficModel>> = (props) => {
  const item = props.item

  return (
    <SelectableItem {...props}>
      <Stack>
        <Text>{item.title}</Text>
        <Text>{item.type}</Text>
      </Stack>
    </SelectableItem>
  )
}

export const TeamItem: FC<SelectableItemProps<TeamTrafficModel>> = (props) => {
  const item = props.item

  return (
    <SelectableItem {...props}>
      <Stack>
        <Text>{item.name}</Text>
        <Text>{item.organization}</Text>
      </Stack>
    </SelectableItem>
  )
}

export const FileItem: FC<SelectableItemProps<FileRecord>> = (props) => {
  const item = props.item

  return (
    <SelectableItem {...props}>
      <Stack>
        <Text>{item.fileName}</Text>
        <Text>{HunamizeSize(item.size ?? 0)}</Text>
        <Text>{dayjs(item.updateTime).format('YYYY-MM-DD HH:mm:ss')}</Text>
      </Stack>
    </SelectableItem>
  )
}

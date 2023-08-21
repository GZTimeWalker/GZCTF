import dayjs from 'dayjs'
import { FC } from 'react'
import { Avatar, Badge, Group, Stack, Text, rem, useMantineTheme } from '@mantine/core'
import { Icon } from '@mdi/react'
import { SelectableItemProps, SelectableItem } from '@Components/ScrollSelect'
import { ChallengeTagLabelMap, HunamizeSize } from '@Utils/Shared'
import {
  ChallengeTrafficModel,
  TeamTrafficModel,
  FileRecord,
  ChallengeTag,
  ChallengeType,
} from '@Api'

const itemHeight = rem(60)

export const ChallengeItem: FC<SelectableItemProps<ChallengeTrafficModel>> = (props) => {
  const item = props.item
  const data = ChallengeTagLabelMap.get(item.tag as ChallengeTag)!
  const theme = useMantineTheme()
  const type = item.type === ChallengeType.DynamicContainer ? 'dyn' : 'sta'

  return (
    <SelectableItem h={itemHeight} {...props}>
      <Group position="apart" spacing={0} w="100%" noWrap>
        <Group position="left" spacing="xs" noWrap>
          <Icon path={data.icon} color={theme.colors[data.color ?? 'brand'][5]} size={1} />
          <Stack spacing={0} align="flex-start">
            <Text truncate fw={700} w="calc(25vw - 14rem)">
              {item.title}
            </Text>
            <Badge color={data.color} size="xs" variant="dot">
              {type}
            </Badge>
          </Stack>
        </Group>

        <Text color="dimmed" size="xs">
          {item.count} 队伍
        </Text>
      </Group>
    </SelectableItem>
  )
}

export const TeamItem: FC<SelectableItemProps<TeamTrafficModel>> = (props) => {
  const item = props.item

  return (
    <SelectableItem h={itemHeight} {...props}>
      <Group position="apart" spacing={0} w="100%" noWrap>
        <Group position="left" spacing="xs" noWrap>
          <Avatar
            alt="avatar"
            src={item.avatar}
            radius="xl"
            size={30}
            color="brand"
            sx={(theme) => ({
              ...theme.fn.hover({
                cursor: 'pointer',
              }),
            })}
          >
            {item.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Stack spacing={0} align="flex-start">
            <Text truncate fw={700} w="calc(25vw - 14rem)">
              {item.name ?? 'Team'}
            </Text>
            {item.organization && (
              <Badge size="xs" variant="outline">
                {item.organization}
              </Badge>
            )}
          </Stack>
        </Group>

        <Text color="dimmed" size="xs">
          {item.count} 流量
        </Text>
      </Group>
    </SelectableItem>
  )
}

export const FileItem: FC<SelectableItemProps<FileRecord>> = (props) => {
  const item = props.item

  return (
    <SelectableItem h={itemHeight} {...props}>
      <Group position="apart" spacing={0} w="100%" noWrap>
        <Stack spacing={0} align="flex-start">
          <Text truncate fw={500} w="calc(50vw - 20rem)">
            {item.fileName}
          </Text>
          <Badge size="sm" color="indigo">
            {dayjs(item.updateTime).format('MM/DD HH:mm:ss')}
          </Badge>
        </Stack>

        <Text fw={500} size="sm">
          {HunamizeSize(item.size ?? 0)}
        </Text>
      </Group>
    </SelectableItem>
  )
}

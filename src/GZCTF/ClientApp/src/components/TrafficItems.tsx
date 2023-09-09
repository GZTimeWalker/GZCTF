import dayjs from 'dayjs'
import { Avatar, Badge, Group, rem, Stack, Text, useMantineTheme } from '@mantine/core'
import { mdiFileDownloadOutline, mdiMenuRight } from '@mdi/js'
import { Icon } from '@mdi/react'
import { SelectableItem, SelectableItemComponent } from '@Components/ScrollSelect'
import { ChallengeTagLabelMap, HunamizeSize } from '@Utils/Shared'
import {
  ChallengeTag,
  ChallengeTrafficModel,
  ChallengeType,
  FileRecord,
  TeamTrafficModel,
} from '@Api'

const itemHeight = rem(60)

export const ChallengeItem: SelectableItemComponent<ChallengeTrafficModel> = (itemProps) => {
  const { item, ...props } = itemProps
  const data = ChallengeTagLabelMap.get(item.tag as ChallengeTag)!
  const theme = useMantineTheme()
  const type = item.type === ChallengeType.DynamicContainer ? 'dyn' : 'sta'

  return (
    <SelectableItem h={itemHeight} pr={5} {...props}>
      <Group position="apart" spacing={0} w="100%" noWrap>
        <Group position="left" spacing="xs" noWrap>
          <Icon path={data.icon} color={theme.colors[data.color ?? 'brand'][5]} size={1} />
          <Stack spacing={0} align="flex-start">
            <Text truncate fw={700} w="calc(25vw - 15rem)">
              {item.title}
            </Text>
            <Badge color={data.color} size="xs" variant="dot">
              {type}
            </Badge>
          </Stack>
        </Group>

        <Group position="right" spacing={2} noWrap>
          <Text color="dimmed" size="xs" lineClamp={1}>
            {item.count}&nbsp;队伍
          </Text>
          <Icon path={mdiMenuRight} size={1} />
        </Group>
      </Group>
    </SelectableItem>
  )
}

export const TeamItem: SelectableItemComponent<TeamTrafficModel> = (itemProps) => {
  const { item, ...props } = itemProps

  return (
    <SelectableItem h={itemHeight} pr={5} {...props}>
      <Group position="apart" spacing={0} w="100%" noWrap>
        <Group position="left" spacing="xs" noWrap>
          <Avatar alt="avatar" src={item.avatar} radius="xl" size={30} color="brand">
            {item.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Stack spacing={0} align="flex-start">
            <Text truncate fw={700} w="calc(25vw - 15rem)">
              {item.name ?? 'Team'}
            </Text>
            {item.organization && (
              <Badge size="xs" variant="outline">
                {item.organization}
              </Badge>
            )}
          </Stack>
        </Group>

        <Group position="right" spacing={2} noWrap>
          <Text color="dimmed" size="xs" lineClamp={1}>
            {item.count}&nbsp;流量
          </Text>
          <Icon path={mdiMenuRight} size={1} />
        </Group>
      </Group>
    </SelectableItem>
  )
}

export const FileItem: SelectableItemComponent<FileRecord> = (itemProps) => {
  const { item, ...props } = itemProps

  return (
    <SelectableItem h={itemHeight} {...props}>
      <Group position="apart" spacing={0} w="100%" noWrap>
        <Group position="left" spacing="sm" noWrap>
          <Icon path={mdiFileDownloadOutline} size={1.2} />

          <Stack spacing={0} align="flex-start">
            <Text truncate fw={500} w="calc(50vw - 22rem)">
              {item.fileName}
            </Text>
            <Badge size="sm" color="indigo">
              {dayjs(item.updateTime).format('MM/DD HH:mm:ss')}
            </Badge>
          </Stack>
        </Group>

        <Text fw={500} size="sm">
          {HunamizeSize(item.size ?? 0)}
        </Text>
      </Group>
    </SelectableItem>
  )
}

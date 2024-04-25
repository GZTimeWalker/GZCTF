import { Avatar, Badge, Group, Input, rem, Stack, Text, useMantineTheme } from '@mantine/core'
import { mdiDeleteOutline, mdiFileDownloadOutline, mdiMenuRight } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import {
  PropsWithItem,
  SelectableItem,
  SelectableItemComponent,
  SelectableItemProps,
} from '@Components/ScrollSelect'
import { useChallengeTagLabelMap, HunamizeSize } from '@Utils/Shared'
import {
  ChallengeTag,
  ChallengeTrafficModel,
  ChallengeType,
  FileRecord,
  TeamTrafficModel,
} from '@Api'
import { ActionIconWithConfirm } from './ActionIconWithConfirm'

const itemHeight = rem(60)

export const ChallengeItem: SelectableItemComponent<ChallengeTrafficModel> = (itemProps) => {
  const { item, ...props } = itemProps
  const challengeTagLabelMap = useChallengeTagLabelMap()
  const data = challengeTagLabelMap.get(item.tag as ChallengeTag)!
  const theme = useMantineTheme()
  const type = item.type === ChallengeType.DynamicContainer ? 'dyn' : 'sta'

  const { t } = useTranslation()

  return (
    <SelectableItem h={itemHeight} pr={5} {...props}>
      <Group position="apart" spacing={0} w="100%" noWrap>
        <Group position="left" spacing="xs" noWrap>
          <Icon path={data.icon} color={theme.colors[data.color ?? 'brand'][5]} size={1} />
          <Stack spacing={0} align="flex-start">
            <Input
              variant="unstyled"
              value={item.title ?? 'Team'}
              readOnly
              sx={() => ({
                input: {
                  userSelect: 'none',
                  lineHeight: 1,
                  fontWeight: 700,
                  height: '1.5rem',
                },
              })}
            />
            <Badge color={data.color} size="xs" variant="dot">
              {type}
            </Badge>
          </Stack>
        </Group>

        <Group position="right" spacing={2} noWrap w="6rem">
          <Text color="dimmed" size="xs" lineClamp={1}>
            {item.count}&nbsp;{t('common.label.team')}
          </Text>
          <Icon path={mdiMenuRight} size={1} />
        </Group>
      </Group>
    </SelectableItem>
  )
}

export const TeamItem: SelectableItemComponent<TeamTrafficModel> = (itemProps) => {
  const { item, ...props } = itemProps

  const { t } = useTranslation()

  return (
    <SelectableItem h={itemHeight} pr={5} {...props}>
      <Group position="apart" spacing={0} w="100%" noWrap>
        <Group position="left" spacing="xs" noWrap>
          <Avatar alt="avatar" src={item.avatar} radius="xl" size={30} color="brand">
            {item.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Stack spacing={0} align="flex-start">
            <Input
              variant="unstyled"
              value={item.name ?? 'Team'}
              readOnly
              sx={() => ({
                input: {
                  userSelect: 'none',
                  lineHeight: 1,
                  fontWeight: 700,
                  height: '1.5rem',
                },
              })}
            />
            {item.organization && (
              <Badge size="xs" variant="outline">
                {item.organization}
              </Badge>
            )}
          </Stack>
        </Group>

        <Group position="right" spacing={2} noWrap w="6rem">
          <Text color="dimmed" size="xs" lineClamp={1}>
            {item.count}&nbsp;{t('game.label.traffic')}
          </Text>
          <Icon path={mdiMenuRight} size={1} />
        </Group>
      </Group>
    </SelectableItem>
  )
}

export interface FileItemProps extends SelectableItemProps {
  t: (key: string) => string
  disabled: boolean
  onDownload: (file: FileRecord) => void
  onDelete: (file: FileRecord) => Promise<void>
}

export const FileItem: FC<PropsWithItem<FileItemProps, FileRecord>> = (itemProps) => {
  const { item, onDownload, onDelete, disabled, t, ...props } = itemProps

  return (
    <SelectableItem h={itemHeight} active={false} {...props}>
      <Group position="apart" spacing={0} noWrap w="100%">
        <Group
          position="apart"
          spacing={0}
          noWrap
          w="calc(100% - 2.5rem)"
          onClick={() => onDownload(item)}
        >
          <Group position="left" spacing="sm" noWrap>
            <Icon path={mdiFileDownloadOutline} size={1.2} />

            <Stack spacing={0} align="flex-start">
              <Text truncate fw={500}>
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
        <Group position="right" spacing="sm" noWrap w="2.5rem">
          <ActionIconWithConfirm
            iconPath={mdiDeleteOutline}
            color="red"
            message={t('game.content.traffic.delete_confirm')}
            disabled={disabled}
            onClick={() => onDelete(item)}
          />
        </Group>
      </Group>
    </SelectableItem>
  )
}

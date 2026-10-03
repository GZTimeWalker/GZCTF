import { Avatar, Badge, Group, rem, Stack, Text, useMantineTheme } from '@mantine/core'
import { mdiDeleteOutline, mdiFileDownloadOutline, mdiMenuRight } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { PropsWithItem, SelectableItem, SelectableItemComponent, SelectableItemProps } from '@Components/ScrollSelect'
import { useChallengeCategoryLabelMap, HunamizeSize } from '@Utils/Shared'
import { ChallengeCategory, ChallengeTrafficModel, ChallengeType, FileRecord, TeamTrafficModel } from '@Api'
import { ActionIconWithConfirm } from './ActionIconWithConfirm'
import { ScrollingText } from './ScrollingText'

const itemHeight = rem(60)

export const ChallengeItem: SelectableItemComponent<ChallengeTrafficModel> = (itemProps) => {
  const { item, ...props } = itemProps
  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()
  const data = challengeCategoryLabelMap.get(item.category as ChallengeCategory)!
  const theme = useMantineTheme()
  const type = item.type === ChallengeType.DynamicContainer ? 'dyn' : 'sta'
  const { t } = useTranslation()

  return (
    <SelectableItem h={itemHeight} pr={5} {...props}>
      <Group justify="space-between" gap="sm" w="100%" wrap="nowrap" maw="100%">
        <Group justify="left" gap="xs" wrap="nowrap" miw={0} flex={1}>
          <Icon path={data.icon} color={theme.colors[data.color ?? theme.primaryColor][5]} size={1} />
          <Stack gap={0} align="flex-start" miw={0} flex={1}>
            <ScrollingText text={item.title ?? ''} w="100%" />
            <Badge color={data.color} size="xs" variant="dot">
              {type}
            </Badge>
          </Stack>
        </Group>
        <Group justify="right" gap={2} wrap="nowrap" miw="fit-content" flex="0 0 auto">
          <Text c="dimmed" size="xs">
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
      <Group justify="space-between" gap="sm" w="100%" wrap="nowrap" maw="100%">
        <Group justify="left" gap="xs" wrap="nowrap" miw={0} flex={1}>
          <Avatar alt="avatar" src={item.avatar} radius="xl" size={30}>
            {item.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Stack gap={0} align="flex-start" miw={0} flex={1}>
            <ScrollingText text={item.name ?? ''} w="100%" />
            {item.division && (
              <Badge size="xs" variant="outline">
                {item.division}
              </Badge>
            )}
          </Stack>
        </Group>
        <Group justify="right" gap={2} wrap="nowrap" miw="fit-content" flex="0 0 auto">
          <Text c="dimmed" size="xs">
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
  locale: string
  onDownload: (file: FileRecord) => void
  onDelete: (file: FileRecord) => Promise<void>
}

export const FileItem: FC<PropsWithItem<FileItemProps, FileRecord>> = (itemProps) => {
  const { item, onDownload, onDelete, disabled, t, locale, ...props } = itemProps

  return (
    <SelectableItem h={itemHeight} active={false} {...props}>
      <Group justify="space-between" gap={0} wrap="nowrap" w="100%">
        <Group justify="space-between" gap={0} wrap="nowrap" w="calc(100% - 2.5rem)" onClick={() => onDownload(item)}>
          <Group justify="left" gap="sm" wrap="nowrap">
            <Icon path={mdiFileDownloadOutline} size={1.2} />

            <Stack gap={0} align="flex-start">
              <Text truncate fw={500}>
                {item.fileName}
              </Text>
              <Badge size="sm" color="indigo">
                {dayjs(item.updateTime).locale(locale).format('SL LTS')}
              </Badge>
            </Stack>
          </Group>

          <Text fw={500} size="sm">
            {HunamizeSize(item.size ?? 0)}
          </Text>
        </Group>
        <Group justify="right" gap="sm" wrap="nowrap" w="2.5rem">
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

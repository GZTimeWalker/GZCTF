import { ActionIcon, Badge, Card, CardProps, Group, Stack, Text, Title, Tooltip } from '@mantine/core'
import { mdiContentCopy, mdiDeleteOutline, mdiPencilOutline, mdiTagOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import { ScrollingText } from '@Components/ScrollingText'
import { getPermissionI18nKey, PERMISSION_DEFINITIONS, permissionMaskToArray } from '@Utils/Permission'
import { Division } from '@Api'

export interface DivisionCardProps extends CardProps {
  division: Division
  challengeTitleMap: Map<number, string>
  onEdit: (division: Division) => void
  onDelete: (division: Division) => Promise<void>
  onCopyInviteCode: (code: string) => void
}

export const DivisionCard: FC<DivisionCardProps> = ({
  division,
  challengeTitleMap,
  onEdit,
  onDelete,
  onCopyInviteCode,
  ...cardProps
}) => {
  const { t } = useTranslation()

  const overrides = (division.challengeConfigs ?? []).toSorted((a, b) => {
    const left = challengeTitleMap.get(a.challengeId) ?? `#${a.challengeId}`
    const right = challengeTitleMap.get(b.challengeId) ?? `#${b.challengeId}`
    return left.localeCompare(right)
  })

  const renderPermissionBadges = (mask?: number | null, includeGlobal = false) => {
    const values = permissionMaskToArray(mask)
    if (values.length === 0) {
      return (
        <Badge key="empty" color="gray" variant="light" size="sm">
          {t('admin.content.games.divisions.no_permission')}
        </Badge>
      )
    }

    const filtered = PERMISSION_DEFINITIONS.filter((definition) => includeGlobal || definition.challengeScoped).filter(
      (definition) => values.includes(definition.value)
    )

    if (filtered.length === 0) {
      return (
        <Badge key="empty" color="gray" variant="light" size="sm">
          {t('admin.content.games.divisions.no_permission')}
        </Badge>
      )
    }

    return filtered.map((definition) => (
      <Badge key={definition.value} color={definition.color} variant="light" size="sm">
        {t(getPermissionI18nKey(definition.i18nKey, 'label'))}
      </Badge>
    ))
  }

  return (
    <Card withBorder shadow="sm" radius="md" padding="md" {...cardProps}>
      <Stack gap="xs">
        <Group justify="space-between" align="flex-start">
          <Group gap="sm">
            <Icon path={mdiTagOutline} size={0.8} />
            <Title order={4} lineClamp={1}>
              {division.name}
            </Title>
          </Group>
          <Group gap="sm" wrap="nowrap">
            <ActionIcon variant="transparent" color="blue" onClick={() => onEdit(division)}>
              <Icon path={mdiPencilOutline} size={1} />
            </ActionIcon>
            <ActionIconWithConfirm
              iconPath={mdiDeleteOutline}
              color="red"
              message={t('admin.content.games.divisions.delete_confirm', { name: division.name })}
              onClick={() => onDelete(division)}
            />
          </Group>
        </Group>

        {division.inviteCode && (
          <Group gap="sm" align="center" wrap="wrap">
            <Text size="sm">{t('admin.content.games.divisions.form.invite_code.label')}</Text>
            <Text ff="monospace" fw="bold" fz="sm">
              {division.inviteCode}
            </Text>
            <Tooltip label={t('common.button.copy')} withArrow>
              <ActionIcon size="sm" variant="transparent" onClick={() => onCopyInviteCode(division.inviteCode ?? '')}>
                <Icon path={mdiContentCopy} size={0.6} />
              </ActionIcon>
            </Tooltip>
          </Group>
        )}

        <Group gap="sm">
          <Text size="sm">{t('admin.content.games.divisions.default_permission_label')}</Text>
          {renderPermissionBadges(division.defaultPermissions, true)}
        </Group>

        {overrides.length > 0 && (
          <Stack gap="xs">
            <Text size="sm">{t('admin.content.games.divisions.override_label')}</Text>
            <Stack gap="xs">
              {overrides.map((config) => (
                <Group key={config.challengeId}>
                  <Text size="sm" miw="2rem">{`#${config.challengeId}`}</Text>
                  <ScrollingText text={challengeTitleMap.get(config.challengeId) ?? ''} w="14rem" />
                  <Group gap={6} wrap="wrap">
                    {renderPermissionBadges(config.permissions)}
                  </Group>
                </Group>
              ))}
            </Stack>
          </Stack>
        )}
      </Stack>
    </Card>
  )
}

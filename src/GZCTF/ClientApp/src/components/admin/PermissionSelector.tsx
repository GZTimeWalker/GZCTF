import { Box, Checkbox, Group, SimpleGrid, Stack, StackProps, Tooltip } from '@mantine/core'
import { FC, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import {
  getPermissionI18nKey,
  PERMISSION_DEFINITIONS,
  PermissionDefinition,
  permissionMaskToArray,
  permissionsToMask,
} from '@Utils/Permission'
import { GamePermission } from '@Api'
import classes from '@Styles/PermissionDot.module.css'

interface PermissionSelectorProps extends Omit<StackProps, 'onChange' | 'children'> {
  value?: number | null
  challengeScoped?: boolean
  onChange: (value: number) => void
  disabled?: boolean
}

export const PermissionSelector: FC<PermissionSelectorProps> = ({
  value,
  onChange,
  disabled,
  challengeScoped,
  ...props
}) => {
  const { t } = useTranslation()

  const filteredDefinitions = useMemo(() => {
    if (!challengeScoped) {
      return PERMISSION_DEFINITIONS
    }
    return PERMISSION_DEFINITIONS.filter((definition) => definition.challengeScoped)
  }, [challengeScoped])

  const selectedValues = useMemo(() => {
    const available = new Set(filteredDefinitions.map((definition) => definition.value))
    return permissionMaskToArray(value)
      .filter((permission) => available.has(permission))
      .map((permission) => permission.toString())
  }, [value, filteredDefinitions])

  const hasSelection = selectedValues.length > 0
  const allSelected = filteredDefinitions.length > 0 && selectedValues.length === filteredDefinitions.length

  const handleGroupChange = (entries: string[]) => {
    const mask = permissionsToMask(entries.map((entry) => Number(entry) as GamePermission))
    onChange(mask)
  }

  const handleToggleAll = (checked: boolean) => {
    if (!checked) {
      onChange(0)
      return
    }

    if (!challengeScoped) {
      onChange(GamePermission.All)
      return
    }

    onChange(permissionsToMask(filteredDefinitions.map((definition) => definition.value)))
  }

  return (
    <Stack gap="sm" {...props}>
      <Group>
        <Checkbox
          disabled={disabled}
          label={t('admin.content.games.divisions.permissions.all')}
          checked={allSelected}
          indeterminate={!allSelected && hasSelection}
          onChange={(event) => handleToggleAll(event.currentTarget.checked)}
        />
      </Group>
      <Checkbox.Group value={selectedValues} onChange={handleGroupChange}>
        <SimpleGrid spacing="sm" cols={{ base: 1, sm: 2 }}>
          {filteredDefinitions.map((definition) => (
            <Checkbox
              key={definition.value}
              value={definition.value.toString()}
              label={t(getPermissionI18nKey(definition.i18nKey, 'label'))}
              description={t(getPermissionI18nKey(definition.i18nKey, 'description'))}
              disabled={disabled}
              color={definition.color}
            />
          ))}
        </SimpleGrid>
      </Checkbox.Group>
    </Stack>
  )
}

export interface PermissionDotProps extends PermissionDefinition {
  granted?: boolean
}

export const PermissionDot: FC<PermissionDotProps> = ({ granted = true, ...definition }) => {
  const { t } = useTranslation()

  return (
    <Tooltip
      key={definition.value}
      label={t(getPermissionI18nKey(definition.i18nKey, 'label'))}
      withArrow
      position="top"
    >
      <Box
        component="span"
        className={classes.dot}
        data-granted={granted || undefined}
        style={{
          '--dot-color': `var(--mantine-color-${definition.color}-6)`,
        }}
      />
    </Tooltip>
  )
}

import { Group, Paper, Stack, Text, UnstyledButton } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { skillTreeIconKeys, skillTreeIcons, type SkillTreeIconKey } from '@Utils/skillTreeAdmin'

type PresetIconPickerProps = {
  value: SkillTreeIconKey
  onChange: (value: SkillTreeIconKey) => void
  disabled?: boolean
  error?: string
}

export const PresetIconPicker = ({ value, onChange, disabled, error }: PresetIconPickerProps) => {
  const { t } = useTranslation('skillTrees')

  return (
    <Stack gap={4}>
      <Text size="sm" fw={500}>
        {t('icon.label')}
      </Text>
      <Group gap="xs">
        {skillTreeIconKeys.map((key) => (
          <UnstyledButton
            key={key}
            type="button"
            aria-label={t(`icon.${key}`)}
            aria-pressed={value === key}
            disabled={disabled}
            onClick={() => onChange(key)}
          >
            <Paper
              withBorder
              p="xs"
              style={{
                borderColor: value === key ? 'var(--mantine-color-blue-6)' : undefined,
                borderWidth: value === key ? 2 : 1,
                opacity: disabled ? 0.6 : 1,
              }}
            >
              <Text size="xl" aria-hidden>
                {skillTreeIcons[key]}
              </Text>
            </Paper>
          </UnstyledButton>
        ))}
      </Group>
      {error && (
        <Text size="xs" c="red">
          {error}
        </Text>
      )}
    </Stack>
  )
}

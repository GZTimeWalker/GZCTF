import { Button, Group, PasswordInput, Stack, Text, TextInput } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { ChallengeType } from '@Api'
import type { ChallengeDraft } from '@Utils/challengeEditor'

type Props = {
  value: ChallengeDraft
  onChange: (next: ChallengeDraft) => void
  disabled?: boolean
}

export const ChallengeFlagEditor = ({ value, onChange, disabled }: Props) => {
  const { t } = useTranslation('learning')
  const isStatic = value.type === ChallengeType.StaticAttachment || value.type === ChallengeType.StaticContainer

  return (
    <Stack gap="md">
      <Text fw={600}>{t('editorFlags')}</Text>
      {isStatic && (
        <>
          <Text size="sm" c="dimmed">{t('editorStaticFlagHint')}</Text>
          {value.staticFlags.map((flag, index) => (
            <Group key={index} align="flex-end" wrap="nowrap">
              <PasswordInput
                label={t('editorFlagNumber', { number: index + 1 })}
                value={flag}
                onChange={(event) => onChange({
                  ...value,
                  staticFlags: value.staticFlags.map((item, itemIndex) =>
                    itemIndex === index ? event.currentTarget.value : item),
                })}
                disabled={disabled}
                style={{ flex: 1 }}
              />
              <Button
                color="red" variant="subtle" disabled={disabled}
                onClick={() => onChange({ ...value, staticFlags: value.staticFlags.filter((_, itemIndex) => itemIndex !== index) })}
              >{t('editorRemove')}</Button>
            </Group>
          ))}
          <Button variant="light" w="fit-content" disabled={disabled}
            onClick={() => onChange({ ...value, staticFlags: [...value.staticFlags, ''] })}>
            {t('editorAddFlag')}
          </Button>
        </>
      )}
      {value.type === ChallengeType.DynamicContainer && (
        <TextInput
          label={t('editorFlagTemplate')}
          description={t('editorFlagTemplateHint')}
          value={value.container.FlagTemplate}
          onChange={(event) => onChange({
            ...value,
            container: { ...value.container, FlagTemplate: event.currentTarget.value },
          })}
          disabled={disabled}
        />
      )}
      {value.type === ChallengeType.DynamicAttachment && (
        <Text size="sm" c="dimmed">{t('editorDynamicFlagHint')}</Text>
      )}
    </Stack>
  )
}

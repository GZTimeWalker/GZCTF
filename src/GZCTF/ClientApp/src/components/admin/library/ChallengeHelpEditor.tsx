import { Button, Group, Stack, Text, Textarea } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import type { ChallengeDraft } from '@Utils/challengeEditor'

type Props = {
  value: ChallengeDraft
  onChange: (next: ChallengeDraft) => void
  disabled?: boolean
}

export const ChallengeHelpEditor = ({ value, onChange, disabled }: Props) => {
  const { t } = useTranslation('learning')
  return (
    <Stack gap="md">
      <Text fw={600}>{t('editorHelp')}</Text>
      {value.hints.map((hint, index) => (
        <Group key={index} align="flex-end" wrap="nowrap">
          <Textarea
            label={t('editorHintNumber', { number: index + 1 })}
            value={hint}
            onChange={(event) => onChange({
              ...value,
              hints: value.hints.map((item, itemIndex) =>
                itemIndex === index ? event.currentTarget.value : item),
            })}
            disabled={disabled}
            autosize minRows={2}
            style={{ flex: 1 }}
          />
          <Button color="red" variant="subtle" disabled={disabled}
            onClick={() => onChange({ ...value, hints: value.hints.filter((_, itemIndex) => itemIndex !== index) })}>
            {t('editorRemove')}
          </Button>
        </Group>
      ))}
      <Button variant="light" w="fit-content" disabled={disabled}
        onClick={() => onChange({ ...value, hints: [...value.hints, ''] })}>
        {t('editorAddHint')}
      </Button>
      <Textarea
        label={t('editorWriteup')}
        description={t('editorWriteupHint')}
        value={value.writeup}
        onChange={(event) => onChange({ ...value, writeup: event.currentTarget.value })}
        disabled={disabled}
        autosize minRows={6}
      />
    </Stack>
  )
}

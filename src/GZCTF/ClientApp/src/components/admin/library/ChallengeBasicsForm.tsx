import { NumberInput, Select, SimpleGrid, Stack, Switch, Text, Textarea, TextInput } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { ChallengeCategory, Difficulty } from '@Api'
import type { ChallengeDraft } from '@Utils/challengeEditor'

type Props = {
  value: ChallengeDraft
  onChange: (next: ChallengeDraft) => void
  disabled?: boolean
}

export const ChallengeBasicsForm = ({ value, onChange, disabled }: Props) => {
  const { t } = useTranslation('learning')
  const { t: tChallenge } = useTranslation('challenge')
  const set = (patch: Partial<ChallengeDraft>) => onChange({ ...value, ...patch })

  return (
    <Stack gap="md">
      <Text fw={600}>{t('editorBasics')}</Text>
      <TextInput
        label={t('adminEnglishTitle')}
        required
        value={value.title}
        onChange={(event) => set({ title: event.currentTarget.value })}
        disabled={disabled}
      />
      <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }}>
        <Select
          label={t('adminCtfCategory')}
          data={Object.values(ChallengeCategory).map((category) => ({
            value: category,
            label: `${category} · ${tChallenge(`category.${category.toLowerCase()}`)}`,
          }))}
          value={value.ctfCategory}
          onChange={(category) => category && set({ ctfCategory: category as ChallengeCategory })}
          searchable
          disabled={disabled}
        />
        <Select
          label={t('editorDifficulty')}
          data={Object.values(Difficulty).map((difficulty) => ({ value: difficulty, label: difficulty }))}
          value={value.difficulty}
          onChange={(difficulty) => difficulty && set({ difficulty: difficulty as Difficulty })}
          disabled={disabled}
        />
        <NumberInput
          label={t('editorExpectedMinutes')}
          min={1}
          max={1440}
          value={value.expectedMinutes}
          onChange={(minutes) => set({ expectedMinutes: Number(minutes) || 1 })}
          disabled={disabled}
        />
        <NumberInput
          label={t('editorSubmissionLimit')}
          description={t('editorSubmissionLimitHint')}
          min={0}
          value={value.submissionLimit}
          onChange={(limit) => set({ submissionLimit: Math.max(0, Number(limit) || 0) })}
          disabled={disabled}
        />
      </SimpleGrid>
      <TextInput
        label={t('editorSummary')}
        value={value.summary}
        onChange={(event) => set({ summary: event.currentTarget.value })}
        disabled={disabled}
      />
      <Textarea
        label={t('editorDescription')}
        description={t('editorMarkdownHint')}
        minRows={8}
        autosize
        value={value.body}
        onChange={(event) => set({ body: event.currentTarget.value })}
        disabled={disabled}
      />
      <Switch
        label={t('editorEnabled')}
        description={t('editorEnabledHint')}
        checked={value.isEnabled}
        onChange={(event) => set({ isEnabled: event.currentTarget.checked })}
        disabled={disabled}
      />
    </Stack>
  )
}

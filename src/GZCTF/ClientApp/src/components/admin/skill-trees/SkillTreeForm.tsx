import { Button, Stack, Textarea, TextInput } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { PresetIconPicker } from '@Components/admin/skill-trees/PresetIconPicker'
import type { SkillTreeIconKey } from '@Utils/skillTreeAdmin'

export type SkillTreeFormValues = {
  name: string
  summary: string
  iconKey: SkillTreeIconKey
}

type SkillTreeFormProps = {
  values: SkillTreeFormValues
  onChange: (values: SkillTreeFormValues) => void
  onSubmit: () => void
  nameError?: string
  submitLabel?: string
  submitting?: boolean
  disabled?: boolean
}

export const SkillTreeForm = ({
  values,
  onChange,
  onSubmit,
  nameError,
  submitLabel,
  submitting,
  disabled,
}: SkillTreeFormProps) => {
  const { t } = useTranslation('skillTrees')

  return (
    <Stack>
      <TextInput
        label={t('form.name')}
        value={values.name}
        error={nameError}
        disabled={disabled}
        required
        onChange={(event) => onChange({ ...values, name: event.currentTarget.value })}
      />
      <Textarea
        label={t('form.summary')}
        description={t('form.summaryOptional')}
        value={values.summary}
        disabled={disabled}
        minRows={2}
        onChange={(event) => onChange({ ...values, summary: event.currentTarget.value })}
      />
      <PresetIconPicker
        value={values.iconKey}
        disabled={disabled || submitting}
        onChange={(iconKey) => onChange({ ...values, iconKey })}
      />
      <Button loading={submitting} onClick={onSubmit} w="fit-content">
        {submitLabel ?? t('list.create')}
      </Button>
    </Stack>
  )
}

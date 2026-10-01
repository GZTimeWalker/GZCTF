import { ActionIcon, Button, Group, Input, InputWrapperProps, ScrollArea, Stack, TextInput } from '@mantine/core'
import { mdiClose, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'

interface HintListProps extends InputWrapperProps {
  hints: string[]
  hintEnabled: boolean[]
  onChangeHint: (value: string[], enabled: boolean[]) => void
  onToggleHint: (index: number, enabled: boolean) => void
  disabled?: boolean
  height?: number
}

export const HintList: FC<HintListProps> = (props) => {
  const { hints, hintEnabled, onChangeHint, onToggleHint, disabled, height, ...rest } = props

  const { t } = useTranslation()

  const hintdict = hints.map((hint, key) => ({ hint, key, enabled: hintEnabled[key] ?? false }))

  const handleChange = (key: number, value: string) => {
    const newHints = [...hintdict]
    newHints[key].hint = value
    onChangeHint(
      newHints.map((h) => h.hint),
      newHints.map((h) => h.enabled)
    )
  }

  const handleAdd = () => {
    const newHints = [...hintdict, { hint: '', key: hintdict.length, enabled: false }]
    onChangeHint(
      newHints.map((h) => h.hint),
      newHints.map((h) => h.enabled)
    )
  }

  const handleDelete = (key: number) => {
    const newHints = hintdict.filter((h) => h.key !== key)
    onChangeHint(
      newHints.map((h) => h.hint),
      newHints.map((h) => h.enabled)
    )
  }

  return (
    <Input.Wrapper {...rest}>
      <ScrollArea offsetScrollbars scrollbarSize={4} h={height}>
        <Stack gap="xs">
          {hintdict.map((kv) => (
            <Group key={kv.key} mr={4} gap="xs" wrap="nowrap">
              <TextInput
                flex={1}
                miw={0}
                value={kv.hint}
                disabled={disabled}
                aria-label={t('admin.content.games.challenges.hint_label', { count: kv.key + 1 })}
                onChange={(e) => handleChange(kv.key, e.target.value)}
                rightSection={
                  <ActionIcon
                    disabled={disabled}
                    onClick={() => handleDelete(kv.key)}
                    aria-label={t('admin.button.challenges.hint.delete')}
                  >
                    <Icon path={mdiClose} size={1} />
                  </ActionIcon>
                }
              />
              <Button
                size="xs"
                color="teal"
                variant={kv.enabled ? 'filled' : 'outline'}
                disabled={disabled || kv.enabled || !kv.hint.trim()}
                onClick={() => onToggleHint(kv.key, true)}
              >
                {t('admin.button.challenges.hint.enable')}
              </Button>
              <Button
                size="xs"
                color="gray"
                variant={kv.enabled ? 'outline' : 'filled'}
                disabled={disabled || !kv.enabled}
                onClick={() => onToggleHint(kv.key, false)}
              >
                {t('admin.button.challenges.hint.disable')}
              </Button>
            </Group>
          ))}
          <Button mr={4} disabled={disabled} leftSection={<Icon path={mdiPlus} size={1} />} onClick={handleAdd}>
            {t('admin.button.challenges.hint.add')}
          </Button>
        </Stack>
      </ScrollArea>
    </Input.Wrapper>
  )
}

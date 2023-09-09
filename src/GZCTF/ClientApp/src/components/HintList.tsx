import { FC } from 'react'
import {
  ActionIcon,
  Button,
  Input,
  ScrollArea,
  Stack,
  TextInput,
  TextInputProps,
} from '@mantine/core'
import { mdiClose, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'

interface HintListProps extends TextInputProps {
  hints: string[]
  onChangeHint: (value: string[]) => void
  disabled?: boolean
  height?: number
}

export const HintList: FC<HintListProps> = (props) => {
  const { hints, onChangeHint, disabled, height, ...rest } = props

  const hintdict = hints.map((hint, key) => ({ hint, key }))

  const handleChange = (key: number, value: string) => {
    const newHints = [...hintdict]
    newHints[key].hint = value
    onChangeHint(newHints.map((h) => h.hint))
  }

  const handleAdd = () => {
    const newHints = [...hintdict, { hint: '', key: hintdict.length }]
    onChangeHint(newHints.map((h) => h.hint))
  }

  const handleDelete = (key: number) => {
    const newHints = hintdict.filter((h) => h.key !== key)
    onChangeHint(newHints.map((h) => h.hint))
  }

  return (
    <Input.Wrapper {...rest}>
      <ScrollArea offsetScrollbars scrollbarSize={4} h={height}>
        <Stack spacing="xs">
          {hintdict.map((kv) => (
            <TextInput
              mr={4}
              value={kv.hint}
              disabled={disabled}
              key={kv.key}
              onChange={(e) => handleChange(kv.key, e.target.value)}
              rightSection={
                <ActionIcon onClick={() => handleDelete(kv.key)}>
                  <Icon path={mdiClose} size={1} />
                </ActionIcon>
              }
            />
          ))}
          <Button mr={4} leftIcon={<Icon path={mdiPlus} size={1} />} onClick={handleAdd}>
            添加提示
          </Button>
        </Stack>
      </ScrollArea>
    </Input.Wrapper>
  )
}

export default HintList

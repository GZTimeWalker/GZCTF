import React, { FC } from 'react'
import { Text, Box, Center, PasswordInput, Popover, Progress } from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'

const PasswordRequirement: FC<{ meets: boolean; label: string }> = ({ meets, label }) => {
  return (
    <Text color={meets ? 'teal' : 'red'} mt={5} size="sm">
      <Center inline>
        {meets ? <Icon path={mdiCheck} size={0.7} /> : <Icon path={mdiClose} size={0.7} />}
        <Box ml={7}>{label}</Box>
      </Center>
    </Text>
  )
}

const requirements = [
  { re: /[0-9]/, label: '包含数字' },
  { re: /[a-z]/, label: '包含小写字母' },
  { re: /[A-Z]/, label: '包含大写字母' },
  { re: /[$&+,:;=?@#|'<>.^*()%!-`]/, label: '包含特殊字符' },
]

const getStrength = (password: string) => {
  let multiplier = password.length > 5 ? 0 : 1

  requirements.forEach((requirement) => {
    if (!requirement.re.test(password)) {
      multiplier += 1
    }
  })

  return Math.max(100 - (100 / (requirements.length + 1)) * multiplier, 0)
}

interface StrengthPasswordInputProps {
  value: string
  disabled?: boolean
  label?: string
  onChange: React.ChangeEventHandler<HTMLInputElement>
  onKeyDown?: React.KeyboardEventHandler<HTMLInputElement>
}

const StrengthPasswordInput: FC<StrengthPasswordInputProps> = (props) => {
  const [opened, { close, open }] = useDisclosure(false)
  const pwd = props.value

  const checks = [
    <PasswordRequirement key={0} label="至少 6 个字符" meets={pwd.length >= 6} />,
    ...requirements.map((requirement, index) => (
      <PasswordRequirement
        key={index + 1}
        label={requirement.label}
        meets={requirement.re.test(pwd)}
      />
    )),
  ]

  const strength = getStrength(pwd)
  const color = strength === 100 ? 'teal' : strength > 50 ? 'yellow' : 'red'

  return (
    <Popover
      opened={opened}
      position="right"
      styles={{
        dropdown: {
          marginLeft: '2rem',
          width: '10rem',
        },
      }}
      withArrow
      transition="pop-bottom-left"
    >
      <Popover.Target>
        <PasswordInput
          required
          label={props.label ?? '密码'}
          placeholder="P4ssW@rd"
          value={props.value}
          onFocusCapture={open}
          onBlurCapture={close}
          disabled={props.disabled}
          onChange={props.onChange}
          style={{ width: '100%' }}
        />
      </Popover.Target>
      <Popover.Dropdown>
        <Progress color={color} value={strength} size={5} style={{ marginBottom: 10 }} />
        {checks}
      </Popover.Dropdown>
    </Popover>
  )
}

export default StrengthPasswordInput

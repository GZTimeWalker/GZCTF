import { FC, useState } from 'react'
import {
  MantineColor,
  Popover,
  ActionIcon,
  Stack,
  Group,
  Button,
  Text,
  MantineNumberSize,
} from '@mantine/core'
import { Icon } from '@mdi/react'

export interface ActionIconWithConfirmProps {
  iconPath: string
  color?: MantineColor
  size?: MantineNumberSize
  message: string
  disabled?: boolean
  onClick: () => Promise<void>
}

export const ActionIconWithConfirm: FC<ActionIconWithConfirmProps> = (props) => {
  const [opened, setOpened] = useState(false)
  const [loading, setLoading] = useState(false)

  return (
    <Popover shadow="md" width="max-content" position="top" opened={opened} onChange={setOpened}>
      <Popover.Target>
        <ActionIcon
          color={props.color}
          onClick={() => setOpened(true)}
          disabled={props.disabled && !loading}
          size={props.size}
          loading={loading}
        >
          <Icon path={props.iconPath} size={props.size ?? 1} />
        </ActionIcon>
      </Popover.Target>
      <Popover.Dropdown>
        <Stack align="center" spacing={6}>
          <Text
            size="sm"
            fw="bold"
            h="auto"
            ta="center"
            style={{
              whiteSpace: 'pre-wrap',
            }}
          >
            {props.message}
          </Text>

          <Group w="100%" position="apart">
            <Button
              size="xs"
              py={2}
              color={props.color}
              disabled={props.disabled && !loading}
              loading={loading}
              onClick={() => {
                setLoading(true)
                props.onClick().finally(() => {
                  setLoading(false)
                  setOpened(false)
                })
              }}
            >
              确定
            </Button>
            <Button
              size="xs"
              py={2}
              variant="outline"
              disabled={props.disabled}
              onClick={() => setOpened(false)}
            >
              取消
            </Button>
          </Group>
        </Stack>
      </Popover.Dropdown>
    </Popover>
  )
}

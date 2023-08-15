import dayjs from 'dayjs'
import { FC } from 'react'
import { Stack, Text, Button, Group, Code, Tooltip, Anchor } from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import { getProxyUrl } from '@Utils/Shared'
import { useTooltipStyles } from '@Utils/ThemeOverride'
import { ClientFlagContext } from '@Api'
import { Countdown } from './ChallengeDetailModal'

interface InstanceEntryProps {
  context: ClientFlagContext
  disabled: boolean
  onProlong: () => void
  onDestroy: () => void
}

export const InstanceEntry: FC<InstanceEntryProps> = (props) => {
  const { context, onProlong, disabled, onDestroy } = props
  const clipBoard = useClipboard()
  const instanceCloseTime = dayjs(context.closeTime ?? 0)
  const instanceLeft = instanceCloseTime.diff(dayjs(), 'minute')
  const { classes: tooltipClasses, theme } = useTooltipStyles()

  const instanceEntry = context.instanceEntry ?? ''
  const isPlatfromProxy = instanceEntry.length === 36 && !instanceEntry.includes(':')
  const copyEntry = isPlatfromProxy ? getProxyUrl(instanceEntry) : instanceEntry

  return (
    <Stack align="flex-start" spacing={2}>
      <Group noWrap spacing={0} w="100%" position="apart">
        <Text size="sm" fw={600}>
          实例入口：
          <Tooltip label="点击复制" withArrow classNames={tooltipClasses}>
            <Code
              bg="transparent"
              style={{
                fontSize: theme.fontSizes.sm,
                cursor: 'pointer',
              }}
              onClick={() => {
                clipBoard.copy(copyEntry)
                showNotification({
                  color: 'teal',
                  title: isPlatfromProxy ? '实例入口已复制到剪贴板' : undefined,
                  message: isPlatfromProxy ? '请使用客户端进行访问' : '实例入口已复制到剪贴板',
                  icon: <Icon path={mdiCheck} size={1} />,
                })
              }}
            >
              {instanceEntry}
            </Code>
          </Tooltip>
        </Text>
        <Countdown time={context.closeTime ?? '0'} />
      </Group>
      {isPlatfromProxy && (
        <Group noWrap spacing={0}>
          <Text size="sm" fw={600}>
            获取客户端：
            <Anchor
              href="https://github.com/XDSEC/WebSocketReflectorX/releases"
              target="_blank"
              rel="noreferrer"
            >
              WebSocketReflectorX
            </Anchor>
          </Text>
        </Group>
      )}
      <Group position="center" pt="md" w="100%" align="center">
        <Button color="orange" onClick={onProlong} disabled={instanceLeft > 10}>
          延长时间
        </Button>
        <Button color="red" onClick={onDestroy} disabled={disabled}>
          销毁实例
        </Button>
      </Group>
    </Stack>
  )
}

export default InstanceEntry

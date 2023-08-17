import dayjs from 'dayjs'
import { FC } from 'react'
import {
  Stack,
  Text,
  Button,
  Group,
  Tooltip,
  Anchor,
  TextInput,
  ActionIcon,
  Center,
  Divider,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiServerNetwork, mdiContentCopy, mdiOpenInNew, mdiOpenInApp } from '@mdi/js'
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
  const isPlatformProxy = instanceEntry.length === 36 && !instanceEntry.includes(':')
  const copyEntry = isPlatformProxy ? getProxyUrl(instanceEntry) : instanceEntry

  const onCopyEntry = () => {
    clipBoard.copy(copyEntry)
    showNotification({
      color: 'teal',
      title: isPlatformProxy ? '实例入口已复制到剪贴板' : undefined,
      message: isPlatformProxy ? '请使用客户端进行访问' : '实例入口已复制到剪贴板',
      icon: <Icon path={mdiCheck} size={1} />,
    })
  }

  const onOpenInNew = () => {
    window.open(`http://${instanceEntry}`)
  }

  const onOpenInApp = () => {
    if (!isPlatformProxy) {
      return
    }
    const url = new URL('wsrx://open')
    url.searchParams.append('url', copyEntry)
    window.location.href = url.href
    showNotification({
      color: 'teal',
      title: '已尝试拉起客户端',
      message: '请确保客户端正确安装',
      icon: <Icon path={mdiCheck} size={1} />,
    })
  }

  return (
    <Stack spacing={2}>
      <TextInput
        label={<Text fw={600}>实例入口</Text>}
        description={
          isPlatformProxy && (
            <Text>
              平台已启用代理模式，建议使用专用客户端。
              <Anchor
                href="https://github.com/XDSEC/WebSocketReflectorX/releases"
                target="_blank"
                rel="noreferrer"
              >
                获取客户端
              </Anchor>
            </Text>
          )
        }
        icon={<Icon path={mdiServerNetwork} size={1} />}
        value={copyEntry}
        readOnly
        styles={{
          input: {
            fontFamily: `${theme.fontFamilyMonospace}, ${theme.fontFamily}`,
          },
        }}
        rightSection={
          <Group spacing={2}>
            <Divider orientation="vertical" pr={4} />
            <Tooltip label="复制到剪贴板" withArrow classNames={tooltipClasses}>
              <ActionIcon onClick={onCopyEntry}>
                <Icon path={mdiContentCopy} size={1} />
              </ActionIcon>
            </Tooltip>
            <Tooltip
              label={isPlatformProxy ? '在客户端中打开' : '作为网页打开'}
              withArrow
              classNames={tooltipClasses}
            >
              <ActionIcon onClick={isPlatformProxy ? onOpenInApp : onOpenInNew}>
                <Icon path={isPlatformProxy ? mdiOpenInApp : mdiOpenInNew} size={1} />
              </ActionIcon>
            </Tooltip>
          </Group>
        }
        rightSectionWidth="5rem"
      />
      <Center pt="md">
        <Countdown time={context.closeTime ?? '0'} />
      </Center>
      <Group position="center" pt="md" align="center">
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

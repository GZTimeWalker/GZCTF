import dayjs from 'dayjs'
import duration from 'dayjs/plugin/duration'
import { FC, useEffect, useState } from 'react'
import {
  Stack,
  Text,
  Button,
  Group,
  Tooltip,
  Anchor,
  TextInput,
  ActionIcon,
  Divider,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import {
  mdiCheck,
  mdiServerNetwork,
  mdiContentCopy,
  mdiOpenInNew,
  mdiOpenInApp,
  mdiExclamation,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { getProxyUrl } from '@Utils/Shared'
import { useTooltipStyles } from '@Utils/ThemeOverride'
import { ClientFlagContext } from '@Api'

interface InstanceEntryProps {
  test?: boolean
  context: ClientFlagContext
  disabled: boolean
  onCreate?: () => void
  onProlong?: () => void
  onDestroy?: () => void
}

dayjs.extend(duration)

interface CountdownProps {
  time: string
  prolongNotice: () => void
}

const Countdown: FC<CountdownProps> = ({ time, prolongNotice }) => {
  const [now, setNow] = useState(dayjs())
  const end = dayjs(time)
  const countdown = dayjs.duration(end.diff(now))
  const [haveNoticed, setHaveNoticed] = useState(countdown.asMinutes() < 10)

  useEffect(() => {
    if (dayjs() > end) return
    const interval = setInterval(() => setNow(dayjs()), 1000)
    return () => clearInterval(interval)
  }, [])

  useEffect(() => {
    if (countdown.asSeconds() <= 0) return

    if (countdown.asMinutes() < 10 && !haveNoticed) {
      prolongNotice()
      setHaveNoticed(true)
    } else if (countdown.asMinutes() > 10) {
      setHaveNoticed(false)
    }
  }, [countdown])

  return <Text span>{countdown.asSeconds() > 0 ? countdown.format('HH:mm:ss') : '00:00:00'}</Text>
}

export const InstanceEntry: FC<InstanceEntryProps> = (props) => {
  const { test, context, disabled, onCreate, onDestroy } = props

  const clipBoard = useClipboard()

  const [withContainer, setWithContainer] = useState(!!context.instanceEntry)

  const { classes: tooltipClasses, theme } = useTooltipStyles()

  const instanceEntry = context.instanceEntry ?? ''
  const isPlatformProxy = instanceEntry.length === 36 && !instanceEntry.includes(':')
  const copyEntry = isPlatformProxy ? getProxyUrl(instanceEntry, test) : instanceEntry

  const [canProlong, setCanProlong] = useState(false)

  const prolongNotice = () => {
    if (canProlong) return

    showNotification({
      color: 'orange',
      title: '实例即将到期',
      message: '请及时延长时间或销毁实例',
      icon: <Icon path={mdiExclamation} size={1} />,
    })

    setCanProlong(true)
  }

  useEffect(() => {
    setWithContainer(!!context.instanceEntry)
    const countdown = dayjs.duration(dayjs(context.closeTime ?? 0).diff(dayjs()))
    setCanProlong(countdown.asMinutes() < 10)
  }, [context])

  const onProlong = () => {
    if (!canProlong || !props.onProlong) return

    props.onProlong()
    setCanProlong(false)

    showNotification({
      color: 'teal',
      title: '实例时间已延长',
      message: '请注意实例到期时间',
      icon: <Icon path={mdiCheck} size={1} />,
    })
  }

  const onCopyEntry = () => {
    clipBoard.copy(copyEntry)
    showNotification({
      color: 'teal',
      title: isPlatformProxy ? '实例入口已复制到剪贴板' : undefined,
      message: isPlatformProxy ? '请使用客户端进行访问' : '实例入口已复制到剪贴板',
      icon: <Icon path={mdiCheck} size={1} />,
    })
  }

  const getAppUrl = () => {
    const url = new URL('wsrx://open')
    url.searchParams.append('url', copyEntry)
    return url.href
  }

  const openUrl = isPlatformProxy ? getAppUrl() : `http://${instanceEntry}`

  if (!withContainer) {
    return test ? (
      <Text size="md" color="dimmed" fw={600} pt={30}>
        测试容器未开启
      </Text>
    ) : (
      <Group position="apart" pt="xs" noWrap>
        <Stack align="left" spacing={0}>
          <Text size="sm" fw={600}>
            本题为容器题目，解题需开启容器实例
          </Text>
          <Text size="xs" color="dimmed" fw={600}>
            容器默认有效期为 120 分钟
          </Text>
        </Stack>

        <Button onClick={onCreate} disabled={disabled} loading={disabled}>
          开启实例
        </Button>
      </Group>
    )
  }

  return (
    <Stack spacing={2} w="100%">
      <TextInput
        label={<Text fw={600}>实例入口</Text>}
        description={
          isPlatformProxy &&
          !test && (
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
              <ActionIcon
                component="a"
                href={openUrl}
                target={isPlatformProxy ? '_self' : '_blank'}
                rel="noreferrer"
              >
                <Icon path={isPlatformProxy ? mdiOpenInApp : mdiOpenInNew} size={1} />
              </ActionIcon>
            </Tooltip>
          </Group>
        }
        rightSectionWidth="5rem"
      />
      {!test && (
        <Group position="apart" pt="xs" noWrap>
          <Stack align="left" spacing={0}>
            <Text size="sm" fw={600}>
              剩余时间：
              <Countdown time={context.closeTime ?? '0'} prolongNotice={prolongNotice} />
            </Text>
            <Text size="xs" color="dimmed" fw={600}>
              你可以在到期前 10 分钟内延长时间
            </Text>
          </Stack>

          <Group position="right" noWrap spacing="xs">
            <Button color="orange" onClick={onProlong} disabled={!canProlong}>
              延长时间
            </Button>
            <Button color="red" onClick={onDestroy} disabled={disabled}>
              销毁实例
            </Button>
          </Group>
        </Group>
      )}
    </Stack>
  )
}

export default InstanceEntry

import {
  ActionIcon,
  Anchor,
  Button,
  Divider,
  Group,
  Stack,
  Text,
  TextInput,
  Tooltip,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import {
  mdiCheck,
  mdiContentCopy,
  mdiExclamation,
  mdiOpenInApp,
  mdiOpenInNew,
  mdiServerNetwork,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import duration from 'dayjs/plugin/duration'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getProxyUrl } from '@Utils/Shared'
import { useTooltipStyles } from '@Utils/ThemeOverride'
import { useConfig } from '@Utils/useConfig'
import { ClientFlagContext } from '@Api'

interface InstanceEntryProps {
  test?: boolean
  context: ClientFlagContext
  disabled: boolean
  onCreate?: () => void
  onExtend?: () => void
  onDestroy?: () => void
}

dayjs.extend(duration)

interface CountdownProps {
  time: string
  onTimeout?: () => void
  extendEnabled: boolean
  enableExtend: () => void
}

const Countdown: FC<CountdownProps> = (props) => {
  const { time, onTimeout, extendEnabled, enableExtend } = props
  const { config } = useConfig()
  const [now, setNow] = useState(dayjs())
  const end = dayjs(time)
  const countdown = dayjs.duration(end.diff(now))

  useEffect(() => {
    if (dayjs() > end) return
    const interval = setInterval(() => setNow(dayjs()), 1000)
    return () => clearInterval(interval)
  }, [])

  useEffect(() => {
    if (!extendEnabled && config.renewalWindow && countdown.asMinutes() < config.renewalWindow)
      enableExtend()
    if (onTimeout && countdown.asSeconds() <= 0) onTimeout()
  }, [countdown])

  return (
    <Text span fw="bold">
      {countdown.asSeconds() > 0 ? countdown.format('HH:mm:ss') : '00:00:00'}
    </Text>
  )
}

export const InstanceEntry: FC<InstanceEntryProps> = (props) => {
  const { test, context, disabled, onCreate, onDestroy } = props

  const { config } = useConfig()
  const clipBoard = useClipboard()

  const [withContainer, setWithContainer] = useState(!!context.instanceEntry)

  const { classes: tooltipClasses, theme } = useTooltipStyles()

  const instanceEntry = context.instanceEntry ?? ''
  const isPlatformProxy = instanceEntry.length === 36 && !instanceEntry.includes(':')
  const copyEntry = isPlatformProxy ? getProxyUrl(instanceEntry, test) : instanceEntry

  const [canExtend, setCanExtend] = useState(false)

  const { t } = useTranslation()

  const enableExtend = () => {
    if (canExtend) return

    showNotification({
      color: 'orange',
      title: t('challenge.notification.instance.extend.note.title'),
      message: t('challenge.notification.instance.extend.note.message'),
      icon: <Icon path={mdiExclamation} size={1} />,
    })

    setCanExtend(true)
  }

  useEffect(() => {
    setWithContainer(!!context.instanceEntry)
    const countdown = dayjs.duration(dayjs(context.closeTime ?? 0).diff(dayjs()))
    setCanExtend(countdown.asMinutes() < (config.renewalWindow ?? 10))
  }, [context])

  const onExtend = () => {
    if (!canExtend || !props.onExtend) return

    props.onExtend()
    setCanExtend(false)

    showNotification({
      color: 'teal',
      title: t('challenge.notification.instance.extend.success.title'),
      message: t('challenge.notification.instance.extend.success.message'),
      icon: <Icon path={mdiCheck} size={1} />,
    })
  }

  const onCopyEntry = () => {
    clipBoard.copy(copyEntry)
    showNotification({
      color: 'teal',
      title: isPlatformProxy ? t('challenge.notification.instance.copied.url.title') : undefined,
      message: isPlatformProxy
        ? t('challenge.notification.instance.copied.url.message')
        : t('challenge.notification.instance.copied.entry'),
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
      <Text size="md" c="dimmed" fw="bold" pt={30}>
        {t('challenge.content.instance.test.no_container')}
      </Text>
    ) : (
      <Group justify="space-between" pt="xs" wrap="nowrap">
        <Stack align="left" gap={0}>
          <Text size="sm" fw="bold">
            {t('challenge.content.instance.no_container.message')}
          </Text>
          <Text size="xs" c="dimmed" fw="bold">
            {t('challenge.content.instance.no_container.note', {
              min: config.defaultLifetime,
            })}
          </Text>
        </Stack>

        <Button onClick={onCreate} disabled={disabled} loading={disabled}>
          {t('challenge.button.instance.create')}
        </Button>
      </Group>
    )
  }

  return (
    <Stack gap={2} w="100%">
      <TextInput
        label={
          <Text size="sm" fw="bold">
            {t('challenge.content.instance.entry.label')}
          </Text>
        }
        description={
          isPlatformProxy &&
          !test && (
            <Text size="sm">
              {t('challenge.content.instance.entry.description.proxy')}
              <Anchor
                href="https://github.com/XDSEC/WebSocketReflectorX/releases"
                target="_blank"
                rel="noreferrer"
              >
                {t('challenge.content.instance.entry.description.anchor')}
              </Anchor>
            </Text>
          )
        }
        leftSection={<Icon path={mdiServerNetwork} size={1} />}
        value={copyEntry}
        readOnly
        styles={{
          input: {
            fontFamily: theme.fontFamilyMonospace,
          },
        }}
        rightSection={
          <Group gap={2}>
            <Divider orientation="vertical" pr={4} />
            <Tooltip label={t('common.button.copy')} withArrow classNames={tooltipClasses}>
              <ActionIcon onClick={onCopyEntry}>
                <Icon path={mdiContentCopy} size={1} />
              </ActionIcon>
            </Tooltip>
            <Tooltip
              label={
                isPlatformProxy
                  ? t('challenge.content.instance.open.client')
                  : t('challenge.content.instance.open.web')
              }
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
        <Group justify="space-between" pt="xs" wrap="nowrap">
          <Stack align="left" gap={0}>
            <Text size="sm" fw={600}>
              {t('challenge.content.instance.actions.count_down')}
              <Countdown
                time={context.closeTime ?? '0'}
                extendEnabled={canExtend}
                enableExtend={enableExtend}
                onTimeout={onDestroy}
              />
            </Text>
            <Text size="xs" c="dimmed" fw={600}>
              {t('challenge.content.instance.actions.note', { min: config.renewalWindow })}
            </Text>
          </Stack>

          <Group justify="right" wrap="nowrap" gap="xs">
            <Button color="orange" onClick={onExtend} disabled={!canExtend}>
              {t('challenge.button.instance.extend')}
            </Button>
            <Button color="red" onClick={onDestroy} disabled={disabled}>
              {t('challenge.button.instance.destroy')}
            </Button>
          </Group>
        </Group>
      )}
    </Stack>
  )
}

export default InstanceEntry

import { ActionIcon, Anchor, Button, Divider, Group, Stack, Text, TextInput, Tooltip } from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { useDebouncedCallback, useDebouncedState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import {
  mdiCheck,
  mdiContentCopy,
  mdiExclamation,
  mdiOpenInNew,
  mdiServerNetwork,
  mdiTransitConnectionVariant,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { WsrxState } from '@xdsec/wsrx'
import dayjs from 'dayjs'
import duration from 'dayjs/plugin/duration'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { HandleWsrxError, useWsrx } from '@Components/WsrxProvider'
import { getProxyUrl as getProxyEntry } from '@Utils/Shared'
import { useConfig } from '@Hooks/useConfig'
import { ClientFlagContext, ContainerPortMappingType } from '@Api'
import classes from '@Styles/InstanceEntry.module.css'
import misc from '@Styles/Misc.module.css'
import tooltipClasses from '@Styles/Tooltip.module.css'

dayjs.extend(duration)

interface InstanceEntryProps {
  test?: boolean
  label?: string
  context: ClientFlagContext
  disabled?: boolean
  onCreate?: () => void
  onExtend?: () => void
  onDestroy?: () => void
}

interface CountdownProps {
  time?: number | null
  onTimeout?: () => void
  extendEnabled: boolean
  enableExtend: () => void
}

const Countdown: FC<CountdownProps> = (props) => {
  const { time, onTimeout, extendEnabled, enableExtend } = props
  const { config } = useConfig()
  const [now, setNow] = useState(dayjs())
  const end = time ? dayjs(time) : now.add(config.defaultLifetime ?? 120, 'minutes')

  const countdown = dayjs.duration(end.diff(now))

  useEffect(() => {
    if (dayjs() > end) return
    const interval = setInterval(() => setNow(dayjs()), 1000)
    return () => clearInterval(interval)
  }, [])

  useEffect(() => {
    if (!extendEnabled && config.renewalWindow && countdown.asMinutes() < config.renewalWindow) enableExtend()
    if (onTimeout && countdown.asSeconds() <= 0) onTimeout()
  }, [countdown, config.renewalWindow])

  return (
    <Text span fw="bold">
      {countdown.asSeconds() > 0 ? countdown.format('HH:mm:ss') : '00:00:00'}
    </Text>
  )
}

export const InstanceEntry: FC<InstanceEntryProps> = (props) => {
  const { test: isPreview, label, context, disabled, onCreate, onDestroy } = props
  const { wsrx, wsrxState, wsrxOptions } = useWsrx()

  const { config } = useConfig()
  const clipBoard = useClipboard()

  const [forceShowOriginal, setForceShowOriginal] = useState(false)
  const [withContainer, setWithContainer] = useState(!!context.instanceEntry)

  const instanceEntry = context.instanceEntry ?? ''
  const isPlatformProxy =
    config.portMapping === ContainerPortMappingType.PlatformProxy &&
    instanceEntry.length === 36 &&
    !instanceEntry.includes(':')
  const originalEntry = isPlatformProxy ? getProxyEntry(instanceEntry, isPreview) : instanceEntry

  const [canExtend, setCanExtend] = useDebouncedState(false, 500)

  const { t } = useTranslation()

  const enableExtend = useDebouncedCallback(() => {
    showNotification({
      color: 'orange',
      title: t('challenge.notification.instance.extend.note.title'),
      message: t('challenge.notification.instance.extend.note.message'),
      icon: <Icon path={mdiExclamation} size={1} />,
    })
    setCanExtend(true)
  }, 100)

  useEffect(() => {
    setWithContainer(!!context.instanceEntry)
    const countdown = dayjs.duration(dayjs(context.closeTime ?? 0).diff(dayjs()))
    setCanExtend(countdown.asMinutes() < (config.renewalWindow ?? 10))
  }, [context, config.renewalWindow])

  const onExtend = () => {
    if (!canExtend || !props.onExtend) return

    props.onExtend()

    showNotification({
      color: 'teal',
      title: t('challenge.notification.instance.extend.success.title'),
      message: t('challenge.notification.instance.extend.success.message'),
      icon: <Icon path={mdiCheck} size={1} />,
    })

    setCanExtend(false)
  }

  const localTraffic = wsrx.list().find((traffic) => traffic.remote === originalEntry)
  const [localEntry, setLocalEntry] = useState(localTraffic?.local ?? '')

  // is wsrx is ready to use
  const isWsrxUsable = isPlatformProxy && wsrxState === WsrxState.Usable
  // to show original entry
  const useOriginal = !!localTraffic && forceShowOriginal

  useEffect(() => {
    if (!originalEntry || !isWsrxUsable) return

    const localAddr = wsrxOptions.allowLan ? '0.0.0.0:0' : '127.0.0.1:0'

    const requestProxy = async () => {
      try {
        const traffic = await wsrx.add({
          label,
          remote: originalEntry,
          local: localAddr,
        })
        setLocalEntry(traffic.local)
      } catch (err) {
        HandleWsrxError(err, t)
      }
    }

    requestProxy()
  }, [originalEntry, isWsrxUsable, label, wsrxOptions.allowLan])

  const useLocal = isWsrxUsable && !useOriginal
  const entry = useLocal ? localEntry : originalEntry
  const entryIsWss = isPlatformProxy && !useLocal

  const onCopyEntry = () => {
    clipBoard.copy(entry)

    showNotification({
      color: 'teal',
      title: entryIsWss ? t('challenge.notification.instance.copied.url.title') : undefined,
      message: entryIsWss
        ? t('challenge.notification.instance.copied.url.message')
        : t('challenge.notification.instance.copied.entry'),
      icon: <Icon path={mdiCheck} size={1} />,
    })
  }

  if (!withContainer) {
    return isPreview ? (
      <Text size="md" c="dimmed" fw="bold" pt={30}>
        {t('challenge.content.instance.test.no_container')}
      </Text>
    ) : (
      <Group justify="space-between" wrap="nowrap">
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
    <Stack gap="sm" w="100%">
      <TextInput
        label={
          <Text size="sm" fw="bold">
            {t('challenge.content.instance.entry.label')}
          </Text>
        }
        description={
          isPlatformProxy &&
          !isPreview && (
            <Text span size="sm">
              {t('challenge.content.instance.entry.description.proxy')}
              &nbsp;
              <Anchor href="https://github.com/XDSEC/WebSocketReflectorX/releases" target="_blank" rel="noreferrer">
                {t('challenge.content.instance.entry.description.anchor')}
              </Anchor>
            </Text>
          )
        }
        leftSection={
          <Icon
            path={mdiServerNetwork}
            size={1}
            data-proxied={(isWsrxUsable && !useOriginal) || undefined}
            className={classes.icon}
          />
        }
        value={entry}
        readOnly
        classNames={{ input: misc.ffmono }}
        rightSection={
          <Group gap={2}>
            <Divider orientation="vertical" pr={4} />
            {isWsrxUsable && (
              <Tooltip
                label={
                  forceShowOriginal
                    ? t('challenge.button.instance.show.proxied')
                    : t('challenge.button.instance.show.original')
                }
                withArrow
                classNames={tooltipClasses}
              >
                <ActionIcon onClick={() => setForceShowOriginal((prev) => !prev)}>
                  <Icon path={mdiTransitConnectionVariant} size={1} />
                </ActionIcon>
              </Tooltip>
            )}
            <Tooltip label={t('common.button.copy')} withArrow classNames={tooltipClasses}>
              <ActionIcon onClick={onCopyEntry}>
                <Icon path={mdiContentCopy} size={1} />
              </ActionIcon>
            </Tooltip>
            <Tooltip label={t('challenge.content.instance.open.web')} withArrow classNames={tooltipClasses}>
              <ActionIcon
                disabled={entryIsWss}
                component="a"
                href={
                  entryIsWss
                    ? '#'
                    : `http://${useLocal && wsrxOptions.allowLan ? entry.replace('0.0.0.0', '127.0.0.1') : entry}`
                }
                target={entryIsWss ? undefined : '_blank'}
                rel="noreferrer"
              >
                <Icon path={mdiOpenInNew} size={1} />
              </ActionIcon>
            </Tooltip>
          </Group>
        }
        rightSectionWidth={isWsrxUsable ? '6.5rem' : '5rem'}
      />
      {!isPreview && (
        <Group justify="space-between" wrap="nowrap">
          <Stack align="left" gap={0}>
            <Text size="sm" fw={600}>
              {t('challenge.content.instance.actions.count_down')}
              <Countdown
                time={context.closeTime}
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
            <Button color="orange" onClick={onExtend} disabled={!canExtend || disabled}>
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

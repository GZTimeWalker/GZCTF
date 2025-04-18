import { ActionIcon, Group, Stack, Text, TextInput } from '@mantine/core'
import { mdiRefresh, mdiTuneVertical } from '@mdi/js'
import Icon from '@mdi/react'
import { WsrxState } from '@xdsec/wsrx'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useConfig } from '@Hooks/useConfig'
import { DefaultWsrxOptions, HandleWsrxError, useWsrx } from '@Hooks/useWsrx'

/**
 * WsrxManager component
 *
 * Wsrx's state will be managed by this component.
 */
export const WsrxManager: FC<{}> = () => {
  const { wsrx, wsrxOptions, setWsrxOptions } = useWsrx()
  const platformConfig = useConfig()
  const [wsrxState, setWsrxState] = useState(wsrx.getState())
  const { t } = useTranslation()

  wsrx.onStateChange((state) => {
    setWsrxState(state)
  })

  const [showConfig, setShowConfig] = useState(false)

  const doConnect = () => {
    wsrx.connect().catch((err) => HandleWsrxError(err, t))
  }

  useEffect(() => {
    if (platformConfig) {
      setWsrxOptions({
        ...wsrxOptions,
        name: (platformConfig.config.title || 'GZ') + '::CTF',
      })
      doConnect()
    }
  }, [platformConfig.config.title])

  return (
    <Stack gap="xs">
      <Group h="1.5rem" wrap="nowrap" justify="space-between" gap={2}>
        <Text size="sm" fw="bold" c={wsrxState === WsrxState.Usable ? 'green' : 'orange'} flex={1}>
          {wsrxState === WsrxState.Usable
            ? t('wsrx.state.usable')
            : wsrxState === WsrxState.Pending
              ? t('wsrx.state.pending')
              : t('wsrx.state.invalid')}
        </Text>
        <ActionIcon
          variant="subtle"
          aria-label={t('common.button.retry')}
          onClick={doConnect}
          loading={wsrxState === WsrxState.Pending}
        >
          <Icon path={mdiRefresh} size={1} />
        </ActionIcon>
        <ActionIcon variant="subtle" aria-label={t('wsrx.button.config')} onClick={() => setShowConfig(!showConfig)}>
          <Icon path={mdiTuneVertical} size={1} />
        </ActionIcon>
      </Group>
      {showConfig && (
        <TextInput
          size="sm"
          flex={1}
          placeholder={DefaultWsrxOptions.api}
          value={wsrxOptions.api}
          onChange={(e) => setWsrxOptions({ ...wsrxOptions, api: e.currentTarget.value })}
        />
      )}
    </Stack>
  )
}

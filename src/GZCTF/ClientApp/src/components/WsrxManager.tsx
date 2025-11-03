import { ActionIcon, Anchor, Divider, Group, Stack, Switch, Text, TextInput, Tooltip } from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { mdiRefresh, mdiTuneVertical } from '@mdi/js'
import { Icon } from '@mdi/react'
import { WsrxState } from '@xdsec/wsrx'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { DefaultWsrxOptions, useWsrx } from '@Components/WsrxProvider'
import misc from '@Styles/Misc.module.css'

/**
 * WsrxManager component
 *
 * Wsrx's state will be managed by this component.
 */
export const WsrxManager: FC = () => {
  const { wsrxState, wsrxOptions, doWsrxConnect, setWsrxOptions } = useWsrx()
  const { t } = useTranslation()

  const [showConfig, setShowConfig] = useState(false)
  const [option, setOption] = useState(wsrxOptions)
  const [debounced] = useDebouncedValue(option, 300)

  useEffect(() => {
    if (debounced && debounced !== wsrxOptions) {
      setWsrxOptions(debounced)
    }
  }, [debounced, setWsrxOptions])

  return (
    <Stack gap="xs">
      <Group wrap="nowrap" justify="space-between" gap={2}>
        <Stack flex={1} gap={0}>
          <Text size="sm" fw="bold" c={wsrxState === WsrxState.Usable ? 'green' : 'orange'} lineClamp={1} truncate>
            {wsrxState === WsrxState.Usable
              ? t('wsrx.state.usable')
              : wsrxState === WsrxState.Pending
                ? t('wsrx.state.pending')
                : t('wsrx.state.invalid')}
          </Text>
          {wsrxState !== WsrxState.Usable && (
            <Text size="xs" fw="normal">
              <Anchor
                c="dimmed"
                href="https://github.com/XDSEC/WebSocketReflectorX/releases"
                target="_blank"
                rel="noreferrer"
              >
                {t('challenge.content.instance.entry.description.anchor')}
              </Anchor>
            </Text>
          )}
        </Stack>
        <Tooltip label={t('common.button.retry')} withArrow>
          <ActionIcon variant="subtle" onClick={doWsrxConnect} loading={wsrxState === WsrxState.Pending}>
            <Icon path={mdiRefresh} size={1} />
          </ActionIcon>
        </Tooltip>
        <Tooltip label={t('wsrx.button.config')} withArrow>
          <ActionIcon variant="subtle" onClick={() => setShowConfig((prev) => !prev)}>
            <Icon path={mdiTuneVertical} size={1} />
          </ActionIcon>
        </Tooltip>
      </Group>
      {showConfig && (
        <>
          <Divider />
          <TextInput
            size="sm"
            flex={1}
            label={t('wsrx.config.api')}
            placeholder={DefaultWsrxOptions.api}
            value={option.api}
            onChange={(e) => setOption({ ...option, api: e.currentTarget.value })}
          />
          <Switch
            size="sm"
            flex={1}
            classNames={{ body: misc.justifyBetween }}
            labelPosition="left"
            label={
              <Text size="sm" fw={500}>
                {t('wsrx.config.allow_lan')}
              </Text>
            }
            checked={option.allowLan}
            onChange={(e) => setOption({ ...option, allowLan: e.currentTarget.checked })}
          />
        </>
      )}
    </Stack>
  )
}

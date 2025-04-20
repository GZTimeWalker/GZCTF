import { ActionIcon, Anchor, Group, Stack, Text, TextInput, Tooltip } from '@mantine/core'
import { mdiRefresh, mdiTuneVertical } from '@mdi/js'
import Icon from '@mdi/react'
import { WsrxState } from '@xdsec/wsrx'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { DefaultWsrxOptions, useWsrx } from '@Components/WsrxProvider'
import tooltipClasses from '@Styles/Tooltip.module.css'

/**
 * WsrxManager component
 *
 * Wsrx's state will be managed by this component.
 */
export const WsrxManager: FC = () => {
  const { wsrxState, wsrxOptions, doWsrxConnect, setWsrxOptions } = useWsrx()
  const { t } = useTranslation()
  const [showConfig, setShowConfig] = useState(false)

  return (
    <Stack gap="xs">
      <Group wrap="nowrap" justify="space-between" gap={2}>
        <Stack flex={1} gap={0}>
          <Text size="sm" fw="bold" c={wsrxState === WsrxState.Usable ? 'green' : 'orange'}>
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
        <Tooltip label={t('common.button.retry')} withArrow classNames={tooltipClasses}>
          <ActionIcon variant="subtle" onClick={doWsrxConnect} loading={wsrxState === WsrxState.Pending}>
            <Icon path={mdiRefresh} size={1} />
          </ActionIcon>
        </Tooltip>
        <Tooltip label={t('wsrx.button.config')} withArrow classNames={tooltipClasses}>
          <ActionIcon variant="subtle" onClick={() => setShowConfig((prev) => !prev)}>
            <Icon path={mdiTuneVertical} size={1} />
          </ActionIcon>
        </Tooltip>
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

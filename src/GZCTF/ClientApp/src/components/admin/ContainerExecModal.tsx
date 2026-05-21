import { Button, Group, Modal, ModalProps, SegmentedControl, Stack, Text } from '@mantine/core'
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { FitAddon } from '@xterm/addon-fit'
import { Terminal } from '@xterm/xterm'
import '@xterm/xterm/css/xterm.css'
import { FC, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

interface ContainerExecModalProps extends Omit<ModalProps, 'children'> {
  containerGuid?: string | null
  containerTitle?: string
}

/**
 * In-browser terminal over a SignalR ContainerExecHub session.
 * Lifecycle: on open we negotiate a fresh hub connection, invoke
 * `Open(guid, shell)`, then bidirectionally pump bytes via the
 * server's `Stream` (server -> client) and our `Input` calls
 * (client -> server). On close we tell the server `Close(sessionId)`
 * so the underlying docker exec dies immediately rather than waiting
 * for the disconnect.
 */
export const ContainerExecModal: FC<ContainerExecModalProps> = (props) => {
  const { containerGuid, containerTitle, opened, onClose, ...rest } = props
  const { t } = useTranslation()
  const containerRef = useRef<HTMLDivElement | null>(null)
  const termRef = useRef<Terminal | null>(null)
  const fitRef = useRef<FitAddon | null>(null)
  const hubRef = useRef<HubConnection | null>(null)
  const sessionIdRef = useRef<string | null>(null)

  const [shell, setShell] = useState<'sh' | 'bash'>('sh')
  const [status, setStatus] = useState<'idle' | 'connecting' | 'connected' | 'closed' | 'error'>('idle')
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  const writeBytes = (term: Terminal, bytes: Uint8Array) => {
    term.write(bytes)
  }

  const closeSession = async () => {
    const hub = hubRef.current
    const sid = sessionIdRef.current
    if (hub && sid) {
      try { await hub.invoke('Close', sid) } catch { /* ignore */ }
    }
    sessionIdRef.current = null
    try { await hub?.stop() } catch { /* ignore */ }
    hubRef.current = null
  }

  useEffect(() => {
    if (!opened || !containerGuid || !containerRef.current) return
    let disposed = false

    const term = new Terminal({
      fontFamily: 'JetBrains Mono, Consolas, monospace',
      fontSize: 13,
      cursorBlink: true,
      theme: { background: '#0c0c14' },
    })
    const fit = new FitAddon()
    term.loadAddon(fit)
    term.open(containerRef.current)
    fit.fit()
    termRef.current = term
    fitRef.current = fit

    const hub = new HubConnectionBuilder()
      .withUrl('/hub/containerExec')
      .withAutomaticReconnect()
      .build()
    hubRef.current = hub

    const start = async () => {
      setStatus('connecting')
      try {
        await hub.start()
        if (disposed) return
        const sid = await hub.invoke<string>('Open', containerGuid, shell)
        if (disposed) {
          await hub.invoke('Close', sid)
          return
        }
        sessionIdRef.current = sid
        setStatus('connected')

        const { cols, rows } = term
        try { await hub.invoke('Resize', sid, cols, rows) } catch { /* ignore */ }

        // Pump server -> terminal.
        ;(async () => {
          try {
            const sub = hub.stream<number[] | Uint8Array | string>('Stream', sid)
            sub.subscribe({
              next: (chunk) => {
                if (disposed) return
                if (typeof chunk === 'string') {
                  term.write(chunk)
                } else if (chunk instanceof Uint8Array) {
                  writeBytes(term, chunk)
                } else if (Array.isArray(chunk)) {
                  writeBytes(term, Uint8Array.from(chunk))
                }
              },
              error: (err) => {
                if (disposed) return
                setStatus('error')
                setErrorMsg(err?.message ?? String(err))
              },
              complete: () => {
                if (disposed) return
                setStatus('closed')
              },
            })
          } catch (e) {
            if (!disposed) {
              setStatus('error')
              setErrorMsg((e as Error).message)
            }
          }
        })()

        // Pump terminal -> server.
        term.onData((data) => {
          if (!sessionIdRef.current) return
          const bytes = new TextEncoder().encode(data)
          hub.invoke('Input', sessionIdRef.current, Array.from(bytes)).catch(() => { /* ignore */ })
        })

        // Resize handler.
        term.onResize(({ cols, rows }) => {
          if (!sessionIdRef.current) return
          hub.invoke('Resize', sessionIdRef.current, cols, rows).catch(() => { /* ignore */ })
        })
      } catch (e) {
        if (!disposed) {
          setStatus('error')
          setErrorMsg((e as Error).message)
        }
      }
    }

    void start()

    const onResize = () => {
      try { fit.fit() } catch { /* ignore */ }
    }
    window.addEventListener('resize', onResize)

    return () => {
      disposed = true
      window.removeEventListener('resize', onResize)
      void closeSession()
      term.dispose()
      termRef.current = null
      fitRef.current = null
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [opened, containerGuid, shell])

  return (
    <Modal
      size="xl"
      opened={opened}
      onClose={onClose}
      title={
        <Group gap="sm" align="center">
          <Text fw={700}>{t('admin.content.exec.title')}</Text>
          {containerTitle && (
            <Text size="xs" c="dimmed" ff="monospace">
              {containerTitle}
            </Text>
          )}
          <Text
            size="xs"
            c={status === 'connected' ? 'teal' : status === 'error' ? 'red' : 'dimmed'}
          >
            ({status})
          </Text>
        </Group>
      }
      {...rest}
    >
      <Stack gap="sm">
        <Group gap="sm" justify="space-between">
          <SegmentedControl
            size="xs"
            data={['sh', 'bash']}
            value={shell}
            onChange={(v) => setShell(v as 'sh' | 'bash')}
            disabled={status === 'connecting' || status === 'connected'}
          />
          <Button size="xs" variant="default" onClick={() => fitRef.current?.fit()}>
            {t('admin.button.exec.fit')}
          </Button>
        </Group>
        {errorMsg && (
          <Text size="xs" c="red" ff="monospace">
            {errorMsg}
          </Text>
        )}
        <div
          ref={containerRef}
          style={{
            height: '50vh',
            background: '#0c0c14',
            padding: 6,
            borderRadius: 4,
          }}
        />
      </Stack>
    </Modal>
  )
}

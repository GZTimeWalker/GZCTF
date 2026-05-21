import { Alert, Button, Group, Modal, ModalProps, SegmentedControl, Stack, Text } from '@mantine/core'
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr'
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
 * In-browser terminal over the ContainerExecHub SignalR endpoint.
 * On mount: build a HubConnection, invoke Open(guid, shell) to get a
 * session id, subscribe to the Stream IAsyncEnumerable for stdout
 * bytes (base64-encoded over JSON), and pipe Terminal.onData back to
 * the server's Input method (also base64).
 * On unmount: invoke Close(sid) so the docker exec dies immediately
 * instead of waiting for the connection timeout.
 *
 * The `shell` state is intentionally NOT a useEffect dependency —
 * toggling the segmented control after a failed connection shouldn't
 * tear down & rebuild the hub. Pick the shell BEFORE clicking Open;
 * to switch, close the modal and reopen.
 */
export const ContainerExecModal: FC<ContainerExecModalProps> = (props) => {
  const { containerGuid, containerTitle, opened, onClose, ...rest } = props
  const { t } = useTranslation()
  const containerRef = useRef<HTMLDivElement | null>(null)
  const fitRef = useRef<FitAddon | null>(null)
  const hubRef = useRef<HubConnection | null>(null)
  const sessionIdRef = useRef<string | null>(null)
  const shellRef = useRef<'sh' | 'bash'>('sh')

  const [shell, setShell] = useState<'sh' | 'bash'>('sh')
  const [status, setStatus] = useState<'idle' | 'connecting' | 'connected' | 'closed' | 'error'>('idle')
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  // SignalR JSON encodes byte chunks as base64 strings.
  const decodeBase64 = (s: string): Uint8Array => {
    const raw = atob(s)
    const out = new Uint8Array(raw.length)
    for (let i = 0; i < raw.length; i++) out[i] = raw.charCodeAt(i)
    return out
  }

  const encodeBase64 = (bytes: Uint8Array): string => {
    let bin = ''
    for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i])
    return btoa(bin)
  }

  useEffect(() => {
    if (!opened || !containerGuid || !containerRef.current) return
    let disposed = false
    shellRef.current = shell

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
    fitRef.current = fit

    const hub = new HubConnectionBuilder()
      .withUrl('/hub/containerExec')
      .withAutomaticReconnect()
      .build()
    hubRef.current = hub

    const start = async () => {
      setStatus('connecting')
      setErrorMsg(null)
      try {
        await hub.start()
        if (disposed) return
        const sid = await hub.invoke<string>('Open', containerGuid, shellRef.current)
        if (disposed) {
          await hub.invoke('Close', sid).catch(() => undefined)
          return
        }
        sessionIdRef.current = sid
        setStatus('connected')

        const { cols, rows } = term
        hub.invoke('Resize', sid, cols, rows).catch(() => undefined)

        // Server -> terminal pump.
        const sub = hub.stream<string>('Stream', sid)
        sub.subscribe({
          next: (chunk) => {
            if (disposed) return
            try { term.write(decodeBase64(chunk)) }
            catch { /* ignore malformed chunk */ }
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

        // Terminal -> server pump.
        term.onData((data) => {
          if (!sessionIdRef.current) return
          const b64 = encodeBase64(new TextEncoder().encode(data))
          hub.invoke('Input', sessionIdRef.current, b64).catch(() => undefined)
        })

        term.onResize(({ cols: c, rows: r }) => {
          if (!sessionIdRef.current) return
          hub.invoke('Resize', sessionIdRef.current, c, r).catch(() => undefined)
        })
      } catch (e) {
        if (!disposed) {
          setStatus('error')
          setErrorMsg((e as Error).message)
        }
      }
    }

    void start()

    const onWindowResize = () => {
      try { fit.fit() } catch { /* ignore */ }
    }
    window.addEventListener('resize', onWindowResize)

    return () => {
      disposed = true
      window.removeEventListener('resize', onWindowResize)
      const sid = sessionIdRef.current
      const ref = hubRef.current
      sessionIdRef.current = null
      hubRef.current = null
      fitRef.current = null
      if (ref && sid) ref.invoke('Close', sid).catch(() => undefined)
      ref?.stop().catch(() => undefined)
      term.dispose()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [opened, containerGuid])

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
        {status === 'error' && errorMsg && (
          <Alert color="red" variant="light" title={t('admin.content.exec.error_title', 'Connection error')}>
            <Text size="xs" ff="monospace">{errorMsg}</Text>
          </Alert>
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

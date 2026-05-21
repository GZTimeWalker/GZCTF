import { Badge, Center, Group, Loader, SegmentedControl, Stack, Text } from '@mantine/core'
import dayjs from 'dayjs'
import { FC, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { HexAsciiView, ViewMode } from './HexAsciiView'
import api, { TrafficFlowChunk, TrafficFlowDetail, TrafficFlowDirection } from '@Api'
import { useUrlState } from '@Hooks/useUrlState'
import { HunamizeSize } from '@Utils/Shared'

interface FlowDetailProps {
  challengeId: number
  participationId: number
  filename: string
  connectionPort: number | null
}

const decodeBase64 = (s: string): Uint8Array => {
  const binary = atob(s)
  const bytes = new Uint8Array(binary.length)
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i)
  return bytes
}

const concatChunks = (
  chunks: TrafficFlowChunk[],
  direction: TrafficFlowDirection
): { bytes: Uint8Array; flagOffsets: number[] } => {
  const buffers: Uint8Array[] = []
  const flagOffsets: number[] = []
  let cursor = 0
  for (const c of chunks) {
    if (c.direction !== direction) continue
    const bytes = decodeBase64(c.payloadBase64)
    buffers.push(bytes)
    for (const off of c.flagOffsets) flagOffsets.push(cursor + off)
    cursor += bytes.length
  }
  let total = 0
  for (const b of buffers) total += b.length
  const out = new Uint8Array(total)
  let pos = 0
  for (const b of buffers) {
    out.set(b, pos)
    pos += b.length
  }
  return { bytes: out, flagOffsets }
}

export const FlowDetail: FC<FlowDetailProps> = ({
  challengeId,
  participationId,
  filename,
  connectionPort,
}) => {
  const { t } = useTranslation()
  const [detail, setDetail] = useState<TrafficFlowDetail | null>(null)
  const [loading, setLoading] = useState(false)
  const [mode, setMode] = useUrlState<ViewMode>(
    'mode',
    (raw) => (raw === 'hex' ? 'hex' : 'ascii'),
    (v) => (v === 'hex' ? 'hex' : null)
  )

  useEffect(() => {
    if (connectionPort == null) {
      setDetail(null)
      return
    }
    let cancelled = false
    setLoading(true)
    setDetail(null)
    api.game
      .gameGetTrafficFlowDetail(challengeId, participationId, filename, connectionPort)
      .then((res) => {
        if (!cancelled) setDetail(res.data)
      })
      .catch(() => {
        if (!cancelled) setDetail(null)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [challengeId, participationId, filename, connectionPort])

  const out = useMemo(() => (detail ? concatChunks(detail.chunks, 'TeamToContainer') : null), [detail])
  const inn = useMemo(() => (detail ? concatChunks(detail.chunks, 'ContainerToTeam') : null), [detail])

  if (connectionPort == null) {
    return (
      <Center h="100%">
        <Text c="dimmed" size="sm">
          {t('game.label.flow.detail.empty')}
        </Text>
      </Center>
    )
  }

  if (loading) {
    return (
      <Center h="100%">
        <Loader />
      </Center>
    )
  }

  if (!detail) {
    return (
      <Center h="100%">
        <Text c="dimmed" size="sm">
          {t('game.label.flow.detail.empty')}
        </Text>
      </Center>
    )
  }

  return (
    <Stack gap="xs" h="100%">
      <Group justify="space-between" wrap="nowrap">
        <Stack gap={0}>
          <Text size="sm" fw="bold">
            #{detail.connectionPort} · {detail.peerIp}
          </Text>
          <Text size="xs" c="dimmed">
            {dayjs(detail.firstSeenUtc).format('HH:mm:ss.SSS')} →{' '}
            {dayjs(detail.lastSeenUtc).format('HH:mm:ss.SSS')}
          </Text>
        </Stack>
        <Group gap="xs">
          {detail.flagHits > 0 && (
            <Badge color="yellow" variant="filled">
              {t('game.label.flow.column.flag_hits')}: {detail.flagHits}
            </Badge>
          )}
          <SegmentedControl
            size="xs"
            value={mode}
            onChange={(v) => setMode(v as ViewMode)}
            data={[
              { value: 'ascii', label: t('game.label.flow.detail.ascii') },
              { value: 'hex', label: t('game.label.flow.detail.hex') },
            ]}
          />
        </Group>
      </Group>

      <Group align="stretch" grow gap="xs" wrap="nowrap" style={{ flex: 1, minHeight: 0 }}>
        <Stack gap={4} style={{ minWidth: 0 }}>
          <Group justify="space-between">
            <Text size="xs" fw="bold" c="blue">
              ↑ {t('game.label.flow.filter.direction.out')}
            </Text>
            <Text size="xs" c="dimmed">
              {HunamizeSize(out?.bytes.length ?? 0)}
            </Text>
          </Group>
          {out && <HexAsciiView bytes={out.bytes} mode={mode} flagOffsets={out.flagOffsets} />}
        </Stack>
        <Stack gap={4} style={{ minWidth: 0 }}>
          <Group justify="space-between">
            <Text size="xs" fw="bold" c="teal">
              ↓ {t('game.label.flow.filter.direction.in')}
            </Text>
            <Text size="xs" c="dimmed">
              {HunamizeSize(inn?.bytes.length ?? 0)}
            </Text>
          </Group>
          {inn && <HexAsciiView bytes={inn.bytes} mode={mode} flagOffsets={inn.flagOffsets} />}
        </Stack>
      </Group>
    </Stack>
  )
}

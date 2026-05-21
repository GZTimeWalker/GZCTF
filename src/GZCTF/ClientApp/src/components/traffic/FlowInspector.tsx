import {
  Badge,
  Box,
  Center,
  Grid,
  Group,
  Loader,
  Modal,
  ScrollArea,
  SegmentedControl,
  Stack,
  Switch,
  Table,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FlowDetail } from './FlowDetail'
import api, { FlowFilter, TrafficFlowDirection, TrafficFlowSummary } from '@Api'
import { HunamizeSize } from '@Utils/Shared'

interface FlowInspectorProps {
  challengeId: number | null
  participationId: number | null
  filename: string | null
  onClose: () => void
}

type DirectionFilter = 'both' | 'in' | 'out'

const toApiDirection = (d: DirectionFilter): TrafficFlowDirection | undefined =>
  d === 'in' ? 'ContainerToTeam' : d === 'out' ? 'TeamToContainer' : undefined

export const FlowInspector: FC<FlowInspectorProps> = ({
  challengeId,
  participationId,
  filename,
  onClose,
}) => {
  const { t } = useTranslation()

  const opened = challengeId != null && participationId != null && filename != null

  const [regex, setRegex] = useState('')
  const [peerIp, setPeerIp] = useState('')
  const [direction, setDirection] = useState<DirectionFilter>('both')
  const [flagsOnly, setFlagsOnly] = useState(false)

  const [debouncedRegex] = useDebouncedValue(regex, 300)
  const [debouncedPeerIp] = useDebouncedValue(peerIp, 300)

  const [flows, setFlows] = useState<TrafficFlowSummary[]>([])
  const [loading, setLoading] = useState(false)
  const [selected, setSelected] = useState<number | null>(null)

  useEffect(() => {
    if (!opened) return
    let cancelled = false
    setLoading(true)
    setSelected(null)

    const filter: FlowFilter = {
      ...(debouncedRegex ? { regexPattern: debouncedRegex } : {}),
      ...(debouncedPeerIp ? { peerIpContains: debouncedPeerIp } : {}),
      ...(toApiDirection(direction) ? { direction: toApiDirection(direction) } : {}),
      ...(flagsOnly ? { flagsOnly: true } : {}),
    }

    api.game
      .gameGetTrafficFlows(challengeId!, participationId!, filename!, filter)
      .then((res) => {
        if (!cancelled) setFlows(res.data)
      })
      .catch(() => {
        if (!cancelled) setFlows([])
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [opened, challengeId, participationId, filename, debouncedRegex, debouncedPeerIp, direction, flagsOnly])

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      fullScreen
      withCloseButton
      title={
        <Group gap="sm">
          <Title order={4}>{t('game.label.flow.title')}</Title>
          {filename && (
            <Text size="sm" c="dimmed" ff="monospace">
              {filename}
            </Text>
          )}
        </Group>
      }
      styles={{ body: { height: 'calc(100vh - 60px)', padding: 'var(--mantine-spacing-md)' } }}
    >
      <Stack gap="sm" h="100%">
        <Group gap="sm" wrap="nowrap">
          <TextInput
            size="xs"
            placeholder={t('game.label.flow.filter.regex')}
            value={regex}
            onChange={(e) => setRegex(e.currentTarget.value)}
            style={{ flex: 1 }}
          />
          <TextInput
            size="xs"
            placeholder={t('game.label.flow.filter.peer_ip')}
            value={peerIp}
            onChange={(e) => setPeerIp(e.currentTarget.value)}
            w={180}
          />
          <SegmentedControl
            size="xs"
            value={direction}
            onChange={(v) => setDirection(v as DirectionFilter)}
            data={[
              { value: 'both', label: t('game.label.flow.filter.direction.both') },
              { value: 'in', label: t('game.label.flow.filter.direction.in') },
              { value: 'out', label: t('game.label.flow.filter.direction.out') },
            ]}
          />
          <Switch
            size="xs"
            label={t('game.label.flow.filter.flags_only')}
            checked={flagsOnly}
            onChange={(e) => setFlagsOnly(e.currentTarget.checked)}
          />
        </Group>

        <Grid gap={0} style={{ flex: 1, minHeight: 0 }}>
          <Grid.Col span={5} h="100%" style={{ borderRight: '1px solid var(--mantine-color-default-border)' }}>
            <ScrollArea h="100%" type="auto">
              {loading ? (
                <Center py="xl">
                  <Loader size="sm" />
                </Center>
              ) : flows.length === 0 ? (
                <Center py="xl">
                  <Text c="dimmed" size="sm">
                    {t('game.label.flow.empty')}
                  </Text>
                </Center>
              ) : (
                <Table highlightOnHover striped withTableBorder={false} stickyHeader>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th>{t('game.label.flow.column.time')}</Table.Th>
                      <Table.Th>{t('game.label.flow.column.peer')}</Table.Th>
                      <Table.Th>{t('game.label.flow.column.duration')}</Table.Th>
                      <Table.Th>↑</Table.Th>
                      <Table.Th>↓</Table.Th>
                      <Table.Th>🚩</Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {flows.map((flow) => {
                      const dur = dayjs(flow.lastSeenUtc).diff(dayjs(flow.firstSeenUtc), 'millisecond')
                      const isSelected = selected === flow.connectionPort
                      return (
                        <Table.Tr
                          key={flow.connectionPort}
                          onClick={() => setSelected(flow.connectionPort)}
                          style={{
                            cursor: 'pointer',
                            backgroundColor: isSelected ? 'var(--mantine-color-blue-light)' : undefined,
                          }}
                        >
                          <Table.Td ff="monospace" fz="xs">
                            {dayjs(flow.firstSeenUtc).format('HH:mm:ss.SSS')}
                          </Table.Td>
                          <Table.Td ff="monospace" fz="xs">
                            {flow.peerIp}
                          </Table.Td>
                          <Table.Td fz="xs">{dur}ms</Table.Td>
                          <Table.Td fz="xs">{HunamizeSize(flow.bytesOut)}</Table.Td>
                          <Table.Td fz="xs">{HunamizeSize(flow.bytesIn)}</Table.Td>
                          <Table.Td>
                            {flow.flagHits > 0 && (
                              <Badge size="xs" color="yellow" variant="filled">
                                {flow.flagHits}
                              </Badge>
                            )}
                          </Table.Td>
                        </Table.Tr>
                      )
                    })}
                  </Table.Tbody>
                </Table>
              )}
            </ScrollArea>
          </Grid.Col>
          <Grid.Col span={7} h="100%">
            <Box pl="sm" h="100%">
              {opened && (
                <FlowDetail
                  challengeId={challengeId!}
                  participationId={participationId!}
                  filename={filename!}
                  connectionPort={selected}
                />
              )}
            </Box>
          </Grid.Col>
        </Grid>
      </Stack>
    </Modal>
  )
}

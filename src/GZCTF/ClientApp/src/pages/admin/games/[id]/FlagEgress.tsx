import {
  Badge,
  Center,
  Group,
  Loader,
  Paper,
  ScrollArea,
  Stack,
  Table,
  Text,
  TextInput,
  ThemeIcon,
  Tooltip,
} from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { mdiFlagVariantOutline, mdiMagnify } from '@mdi/js'
import { Icon } from '@mdi/react'
import * as signalR from '@microsoft/signalr'
import dayjs from 'dayjs'
import { FC, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import useSWR from 'swr'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { useLanguage } from '@Utils/I18n'
import tableClasses from '@Styles/Table.module.css'

enum FlagEgressDirection {
  ContainerToTeam = 0,
  TeamToContainer = 1,
}

interface FlagEgressEventModel {
  id: number
  gameId: number
  participationId: number
  challengeId: number
  containerId?: string | null
  teamName: string
  challengeTitle: string
  remoteIp: string
  remotePort: number
  hitCount: number
  firstSeenUtc: string
  lastSeenUtc: string
  direction: FlagEgressDirection
}

interface FlagEgressPage {
  data: FlagEgressEventModel[]
  length: number
  total?: number
}

const fetcher = (url: string) =>
  fetch(url, { credentials: 'include' }).then((r) => {
    if (!r.ok) throw new Error('Failed to fetch')
    return r.json()
  })

const directionLabel = (d: FlagEgressDirection) =>
  d === FlagEgressDirection.ContainerToTeam ? 'container → team' : 'team → container'

const directionColor = (d: FlagEgressDirection) =>
  d === FlagEgressDirection.ContainerToTeam ? 'red' : 'orange'

const FlagEgress: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1', 10)
  const { t } = useTranslation()
  const { locale } = useLanguage()
  const [search, setSearch] = useState('')
  const [debounced] = useDebouncedValue(search, 300)
  const liveRef = useRef<FlagEgressEventModel[]>([])
  const [, forceUpdate] = useState(0)

  const { data: page, isLoading, mutate } = useSWR<FlagEgressPage>(
    `/api/admin/Games/${numId}/FlagEgress?count=100`,
    fetcher,
    { refreshInterval: 60_000 }
  )

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hub/admin')
      .withHubProtocol(new signalR.JsonHubProtocol())
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.None)
      .build()

    connection.on('ReceivedFlagEgress', (msg: FlagEgressEventModel) => {
      if (msg.gameId !== numId) return
      const next = [msg, ...liveRef.current.filter((m) => m.id !== msg.id)].slice(0, 200)
      liveRef.current = next
      forceUpdate((n) => n + 1)
    })

    connection.start().catch(() => {
      /* admin hub unavailable — swr polling is the fallback */
    })

    return () => {
      connection.stop().catch(() => undefined)
    }
  }, [numId])

  const merged = [...liveRef.current, ...(page?.data ?? [])]
  // de-dupe by id; live takes precedence
  const seen = new Set<number>()
  const rows = merged.filter((r) => (seen.has(r.id) ? false : (seen.add(r.id), true)))

  const filtered = rows.filter(
    (r) =>
      debounced === '' ||
      r.teamName.toLowerCase().includes(debounced.toLowerCase()) ||
      r.challengeTitle.toLowerCase().includes(debounced.toLowerCase()) ||
      r.remoteIp.toLowerCase().includes(debounced.toLowerCase())
  )

  return (
    <WithGameEditTab
      isLoading={isLoading && !page}
      head={
        <Group justify="space-between" w="100%">
          <TextInput
            w="36%"
            size="sm"
            leftSection={<Icon path={mdiMagnify} size={0.9} />}
            placeholder={t('admin.placeholder.flag_egress.search', 'Filter by team, challenge, or IP…')}
            value={search}
            onChange={(e) => setSearch(e.currentTarget.value)}
          />
          <Group gap="xl">
            <Stack gap={0} align="center">
              <Text fw={700} size="lg" c="red">
                {page?.total ?? rows.length}
              </Text>
              <Text size="xs" c="dimmed">
                {t('admin.label.flag_egress.total_events', 'Egress Events')}
              </Text>
            </Stack>
          </Group>
        </Group>
      }
    >
      {isLoading && !page ? (
        <Center h="60vh">
          <Loader />
        </Center>
      ) : (
        <Paper shadow="md" p="xs" w="100%">
          <ScrollArea offsetScrollbars scrollbarSize={4} h="calc(100vh - 220px)">
            <Table className={tableClasses.table} highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th miw={120}>{t('admin.label.flag_egress.time', 'Last Seen')}</Table.Th>
                  <Table.Th>{t('admin.label.flag_egress.team', 'Team')}</Table.Th>
                  <Table.Th>{t('admin.label.flag_egress.challenge', 'Challenge')}</Table.Th>
                  <Table.Th miw={120}>{t('admin.label.flag_egress.direction', 'Direction')}</Table.Th>
                  <Table.Th miw={160}>{t('admin.label.flag_egress.remote', 'Remote IP:Port')}</Table.Th>
                  <Table.Th miw={80}>{t('admin.label.flag_egress.hits', 'Hits')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {filtered.map((r) => (
                  <Table.Tr key={r.id}>
                    <Table.Td>
                      <Tooltip label={dayjs(r.lastSeenUtc).locale(locale).format('LLL')} withArrow>
                        <Text size="sm" ff="monospace" style={{ cursor: 'help' }}>
                          {dayjs(r.lastSeenUtc).locale(locale).fromNow()}
                        </Text>
                      </Tooltip>
                    </Table.Td>
                    <Table.Td>
                      <Group gap="xs">
                        <ThemeIcon size="xs" color="red" variant="light" radius="xl">
                          <Icon path={mdiFlagVariantOutline} size={0.6} />
                        </ThemeIcon>
                        <Text size="sm" fw={500}>
                          {r.teamName || `#${r.participationId}`}
                        </Text>
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{r.challengeTitle || `#${r.challengeId}`}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Badge size="sm" color={directionColor(r.direction)} variant="light">
                        {directionLabel(r.direction)}
                      </Badge>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" ff="monospace">
                        {r.remoteIp}:{r.remotePort}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" ff="monospace" c={r.hitCount > 10 ? 'red' : undefined}>
                        {r.hitCount}
                      </Text>
                    </Table.Td>
                  </Table.Tr>
                ))}
                {filtered.length === 0 && (
                  <Table.Tr>
                    <Table.Td colSpan={6}>
                      <Text ta="center" c="dimmed" py="md" size="sm">
                        {debounced
                          ? t('admin.placeholder.flag_egress.no_match', 'No events match the filter.')
                          : t('admin.placeholder.flag_egress.empty', 'No flag-egress events yet.')}
                      </Text>
                    </Table.Td>
                  </Table.Tr>
                )}
              </Table.Tbody>
            </Table>
          </ScrollArea>
        </Paper>
      )}
    </WithGameEditTab>
  )
}

export default FlagEgress

import {
    Badge,
    Group,
    Paper,
    ScrollArea,
    Table,
    Text,
    Title,
    Alert,
    SimpleGrid,
    Card,
    ThemeIcon,
    UnstyledButton,
    Center,
    Modal,
    Button,
    Stack,
    TextInput,
    Menu,
    Select,
    Loader,
    Tabs,
    Tooltip,
    Box,
    RingProgress,
    Divider,
    ActionIcon,
    Progress,
    Popover,
} from '@mantine/core'
import { FC, useState, useMemo, useCallback } from 'react'
import { useClipboard } from '@mantine/hooks'
import { Icon } from '@mdi/react'
import {
    mdiCheckCircle,
    mdiGhost,
    mdiIpNetwork,
    mdiArrowUp,
    mdiArrowDown,
    mdiUnfoldMoreHorizontal,
    mdiInformation,
    mdiMagnify,
    mdiShieldAlert,
    mdiChevronDown,
    mdiAccountGroup,
    mdiAlertCircle,
    mdiClockOutline,
    mdiOpenInNew,
    mdiDownload,
    mdiCubeOutline,
    mdiContentCopy,
    mdiCheck,
    mdiFingerprint,
    mdiSwapHorizontal,
    mdiRefresh,
    mdiLockAlert,
    mdiChevronRight,
} from '@mdi/js'
import { useTranslation } from 'react-i18next'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
dayjs.extend(relativeTime)
import { useLanguage } from '@Utils/I18n'
import { useParticipationStatusMap, showErrorMsg } from '@Utils/Shared'
import { showNotification } from '@mantine/notifications'
import { ScrollingText } from '@Components/ScrollingText'
import tableClasses from '@Styles/Table.module.css'
import type { CheatReport, SequenceSuspectDetail, SuspicionRecordResult, CollusionGroupResult, CollusionTeamInfo } from '@Api'
import api, { ParticipationStatus } from '@Api'
import { useParams } from 'react-router'
import classes from './CheatInfo.module.css'
import { useDisclosure } from '@mantine/hooks'

interface CheatInfoProps {
    report: CheatReport | null
    mutate?: () => void
}

interface SortConfig<T> {
    key: keyof T | null
    direction: 'asc' | 'desc'
}

function sortData<T>(data: T[], { key, direction }: SortConfig<T>) {
    // ... existing sortData ...
    if (!key) return data

    return [...data].sort((a, b) => {
        const valueA = a[key]
        const valueB = b[key]

        if (valueA === valueB) return 0

        const compare = valueA < valueB ? -1 : 1
        return direction === 'asc' ? compare : -compare
    })
}

// ... ThSort ...
interface ThSortProps {
    children: React.ReactNode
    reversed: boolean
    sorted: boolean
    onSort(): void
    w?: string | number
    miw?: string | number
}

function ThSort({ children, reversed, sorted, onSort, w, miw }: ThSortProps) {
    const IconPath = sorted ? (reversed ? mdiArrowUp : mdiArrowDown) : mdiUnfoldMoreHorizontal
    return (
        <Table.Th w={w} miw={miw ?? w}>
            <UnstyledButton onClick={onSort} className={classes.control}>
                <Group justify="space-between">
                    <Text fw={700} fz="sm">
                        {children}
                    </Text>
                    <Center className={classes.icon}>
                        <Icon path={IconPath} size={0.7} />
                    </Center>
                </Group>
            </UnstyledButton>
        </Table.Th>
    )
}

interface DetailLine {
    label?: string
    value: string
}

const IP_TYPE_META: Record<string, { label: string; color: string; icon: string }> = {
    SharedIP: { label: 'Shared IP', color: 'orange', icon: mdiIpNetwork },
    SharedFingerprint: { label: 'Shared Fingerprint', color: 'violet', icon: mdiFingerprint },
    FingerprintChurn: { label: 'FP Churn', color: 'yellow', icon: mdiRefresh },
    IpChurn: { label: 'IP Churn', color: 'yellow', icon: mdiRefresh },
    CrossTeamIP: { label: 'Cross-Team IP', color: 'red', icon: mdiSwapHorizontal },
    TokenAbuse: { label: 'Token Abuse', color: 'red', icon: mdiLockAlert },
}

const parseDetailLines = (details?: string | null): DetailLine[] => {
    if (!details) return []

    const rawLines = details.includes('\n')
        ? details.split('\n')
        : details.includes(':')
            ? details.split(/[;|]/)
            : details.split(/\. (?=[A-Z])/)

    return rawLines
        .map((line) => line.trim())
        .filter(Boolean)
        .map((line) => {
            if (/^target\s+/i.test(line)) {
                return { label: 'Target', value: line.replace(/^target\s+/i, '').trim() }
            }

            const idx = line.indexOf(':')
            if (idx > 0) {
                return {
                    label: line.slice(0, idx).trim(),
                    value: line.slice(idx + 1).trim(),
                }
            }

            return { value: line }
        })
}

const CopyButton: FC<{ value: string }> = ({ value }) => {
    const clipboard = useClipboard({ timeout: 1500 })
    return (
        <Tooltip label={clipboard.copied ? 'Copied!' : 'Copy'} withArrow position="top">
            <ActionIcon
                size={14}
                variant="subtle"
                color={clipboard.copied ? 'green' : 'gray'}
                onClick={(e) => { e.stopPropagation(); clipboard.copy(value) }}
                style={{ flexShrink: 0 }}
            >
                <Icon path={clipboard.copied ? mdiCheck : mdiContentCopy} size={0.5} />
            </ActionIcon>
        </Tooltip>
    )
}

const ReadableDetails: FC<{ details?: string | null; maxRows?: number }> = ({ details, maxRows = 3 }) => {
    const lines = parseDetailLines(details)
    if (lines.length === 0) return <Text size="xs" c="dimmed">—</Text>

    // Find summary line (first unlabeled or label='Summary')
    const summaryLine = lines.find(l => !l.label || l.label.toLowerCase() === 'summary')
    const kvLines = lines.filter(l => l !== summaryLine && l.label)
    const visibleKv = kvLines.slice(0, maxRows)
    const hiddenKv = kvLines.slice(maxRows)
    const hasMore = hiddenKv.length > 0

    const renderKvLine = (line: DetailLine, idx: number) => (
        <Group key={idx} gap={4} align="flex-start" wrap="nowrap" style={{ minWidth: 0 }}>
            <Text size="xs" fw={600} c="dimmed" style={{ minWidth: 68, flexShrink: 0, lineHeight: 1.4 }}>
                {line.label}
            </Text>
            <Tooltip label={line.value} withArrow multiline maw={340} disabled={line.value.length <= 40}>
                <Text
                    size="xs"
                    c="dimmed"
                    style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', flex: 1, lineHeight: 1.4 }}
                >
                    {line.value}
                </Text>
            </Tooltip>
            {line.value.length > 20 && <CopyButton value={line.value} />}
        </Group>
    )

    return (
        <Stack gap={2} className={classes.detailsBox} style={{ maxWidth: '100%', overflow: 'hidden' }}>
            {/* Summary line — prominent */}
            {summaryLine && (
                <Text size="xs" fw={700} c="blue.4" style={{ lineHeight: 1.4 }}>
                    {summaryLine.value}
                </Text>
            )}
            {/* Key-value lines */}
            {visibleKv.map(renderKvLine)}
            {/* Expand popover for hidden lines */}
            {hasMore && (
                <Popover width={400} position="bottom-end" withArrow shadow="lg" withinPortal>
                    <Popover.Target>
                        <Group gap={3} style={{ cursor: 'pointer', userSelect: 'none' }} align="center">
                            <Icon path={mdiChevronRight} size={0.55} color="var(--mantine-color-blue-5)" />
                            <Text size="xs" c="blue" fw={600}>
                                +{hiddenKv.length} more fields
                            </Text>
                        </Group>
                    </Popover.Target>
                    <Popover.Dropdown p="sm">
                        <Stack gap={6}>
                            <Group justify="space-between" pb={4} mb={2} style={{ borderBottom: '1px solid var(--mantine-color-dark-4)' }}>
                                <Text size="xs" fw={700} c="dimmed">All Fields</Text>
                                {summaryLine && <Text size="xs" c="blue.4" fw={600}>{summaryLine.value}</Text>}
                            </Group>
                            {kvLines.map(renderKvLine)}
                        </Stack>
                    </Popover.Dropdown>
                </Popover>
            )}
        </Stack>
    )
}

const UsersCell: FC<{ users?: string[]; relatedUsers?: string[] }> = ({ users, relatedUsers }) => {
    const currentUsers = (users ?? []).filter(Boolean)
    const others = (relatedUsers ?? []).filter(Boolean)

    if (currentUsers.length === 0 && others.length === 0) {
        return <Text size="xs" c="dimmed">-</Text>
    }

    const visible = [...currentUsers, ...others].slice(0, 3)
    const hidden = [...currentUsers, ...others].slice(3)

    return (
        <Group gap={4} wrap="wrap" className={classes.userWrap}>
            {visible.map((user, i) => (
                <Badge
                    key={i}
                    size="xs"
                    color={currentUsers.includes(user) ? 'blue' : 'gray'}
                    variant="light"
                    style={{ maxWidth: 120, overflow: 'hidden', textOverflow: 'ellipsis' }}
                    title={user}
                >
                    {user}
                </Badge>
            ))}
            {hidden.length > 0 && (
                <Popover width={260} position="top" withArrow shadow="md">
                    <Popover.Target>
                        <Badge size="xs" color="gray" variant="outline" style={{ cursor: 'pointer' }}>
                            +{hidden.length} more
                        </Badge>
                    </Popover.Target>
                    <Popover.Dropdown>
                        <Text size="xs" fw={700} c="dimmed" mb={4}>All Users</Text>
                        <Group gap={4} wrap="wrap">
                            {[...currentUsers, ...others].map((user, i) => (
                                <Badge key={i} size="xs" color={currentUsers.includes(user) ? 'blue' : 'gray'} variant="light">{user}</Badge>
                            ))}
                        </Group>
                    </Popover.Dropdown>
                </Popover>
            )}
        </Group>
    )
}

export const CheatInfo: FC<CheatInfoProps> = ({ report, mutate }) => {
    const { t } = useTranslation()
    const { locale } = useLanguage()
    const statusMap = useParticipationStatusMap()
    const params = useParams()
    const gameId = parseInt(params.id || '0')

    // 1. IP Analysis Sort State
    const [ipSort, setIpSort] = useState<SortConfig<any>>({ key: null, direction: 'asc' })

    // 2. Abnormal Solves Sort State
    const [solveSort, setSolveSort] = useState<SortConfig<any>>({ key: null, direction: 'asc' })

    // 3. Suspicion Sort State
    const [suspSort, setSuspSort] = useState<SortConfig<any>>({ key: 'score', direction: 'desc' })

    const [opened, { open, close }] = useDisclosure(false)
    const [selectedGroup, setSelectedGroup] = useState<CollusionGroupResult | null>(null)

    // Suspicion Modal
    const [susOpened, { open: openSus, close: closeSus }] = useDisclosure(false)
    const [selectedSuspicion, setSelectedSuspicion] = useState<SuspicionRecordResult | null>(null)

    // Search states
    const [ipSearch, setIpSearch] = useState('')
    const [solveSearch, setSolveSearch] = useState('')
    const [collusionSearch, setCollusionSearch] = useState('')
    const [suspSearch, setSuspSearch] = useState('')

    // Pair selection for drill-down
    const [teamAId, setTeamAId] = useState<number | null>(null)
    const [teamBId, setTeamBId] = useState<number | null>(null)
    const [activeTab, setActiveTab] = useState<string | null>('suspicion')

    const { data: drilledSolves, isLoading: isDrilling } = api.cheatReport.useCheatReportCompare(
        gameId,
        teamAId,
        teamBId
    )

    // 5. Collusion Group Sort State
    const [collusionSort, setCollusionSort] = useState<SortConfig<any>>({ key: 'averageRsi', direction: 'desc' })

    const handleViewDetails = (item: CollusionGroupResult) => {
        setSelectedGroup(item)
        if (item.teams && item.teams.length >= 2) {
            setTeamAId(item.teams[0].participationId ?? 0)
            setTeamBId(item.teams[1].participationId ?? 0)
        } else {
            setTeamAId(null)
            setTeamBId(null)
        }
        open()
    }

    const handleViewSuspicion = (item: SuspicionRecordResult) => {
        setSelectedSuspicion(item)
        openSus()
    }

    const sortedIpAnalysis = useMemo(() => {
        if (!report?.ipAnalysis) return []
        let data = report.ipAnalysis
        if (ipSearch) {
            const q = ipSearch.toLowerCase()
            data = data.filter((item: any) =>
                item.teamName?.toLowerCase().includes(q) ||
                item.type?.toLowerCase().includes(q) ||
                item.ip?.toLowerCase().includes(q) ||
                item.details?.toLowerCase().includes(q) ||
                item.userNames?.some((u: string) => u.toLowerCase().includes(q)) ||
                item.relatedUsers?.some((u: string) => u.toLowerCase().includes(q)) ||
                item.relatedTeams?.some((t: string) => t.toLowerCase().includes(q))
            )
        }
        return sortData(data, ipSort)
    }, [report?.ipAnalysis, ipSort, ipSearch])

    const sortedAbnormalSolves = useMemo(() => {
        if (!report?.abnormalSolves) return []
        let data = report.abnormalSolves
        if (solveSearch) {
            const q = solveSearch.toLowerCase()
            data = data.filter((item: any) =>
                item.teamName?.toLowerCase().includes(q) ||
                item.challengeName?.toLowerCase().includes(q) ||
                item.type?.toLowerCase().includes(q) ||
                item.details?.toLowerCase().includes(q)
            )
        }
        return sortData(data, solveSort)
    }, [report?.abnormalSolves, solveSort, solveSearch])



    const sortedCollusionGroups = useMemo(() => {
        if (!report?.collusionGroups) return []
        let data = report.collusionGroups
        if (collusionSearch) {
            const q = collusionSearch.toLowerCase()
            data = data.filter((item: any) =>
                item.teams?.some((t: CollusionTeamInfo) => t.name?.toLowerCase().includes(q)) ||
                item.details?.toLowerCase().includes(q)
            )
        }
        return sortData(data, collusionSort)
    }, [report?.collusionGroups, collusionSort, collusionSearch])

    const sortedSuspicionList = useMemo(() => {
        if (!report?.suspicionList) return []
        let data = report.suspicionList
        if (suspSearch) {
            const q = suspSearch.toLowerCase()
            data = data.filter((item: any) =>
                item.teamName?.toLowerCase().includes(q)
            )
        }
        return sortData(data, suspSort)
    }, [report?.suspicionList, suspSort, suspSearch])

    const handleSort = (setSort: any, currentSort: any, key: string) => {
        const direction = currentSort.key === key && currentSort.direction === 'asc' ? 'desc' : 'asc'
        setSort({ key, direction })
    }

    const compactHeight = 'clamp(320px, calc(100vh - 24rem), 72vh)'
    const roomyHeight = 'clamp(420px, calc(100vh - 20rem), 82vh)'
    const suspicionTableMinWidth = '44rem'
    const ipTableMinWidth = '84rem'
    const solveTableMinWidth = '76rem'
    const collusionTableMinWidth = '82rem'

    return (
        <>
            <Modal
                opened={opened}
                onClose={close}
                title={
                    <Group gap="xs">
                        <ThemeIcon size="sm" color="grape" variant="light" radius="sm">
                            <Icon path={mdiAccountGroup} size={0.7} />
                        </ThemeIcon>
                        <Text fw={700}>Collusion Details</Text>
                    </Group>
                }
                size="xl"
                centered
            >
                {selectedGroup && (
                    <Stack>
                        <Group grow align="flex-end">
                            <Select
                                label="Team A"
                                data={selectedGroup.teams
                                    ?.filter(t => t.participationId !== teamBId)
                                    .map(t => ({ value: t.participationId?.toString() || '', label: t.name }))}
                                value={teamAId?.toString()}
                                onChange={(val) => setTeamAId(val ? parseInt(val) : null)}
                                searchable
                            />
                            <Center h={60}>
                                <Stack align="center" gap={0}>
                                    <Text size="xl" fw={900} c={((drilledSolves?.rsi ?? selectedGroup.averageRsi ?? 0) > 0.9) ? 'red' : 'yellow'}>
                                        {((drilledSolves?.rsi ?? selectedGroup.averageRsi ?? 0) * 100).toFixed(1)}%
                                    </Text>
                                    <Text size="xs" c="dimmed">Similarity</Text>
                                </Stack>
                            </Center>
                            <Select
                                label="Team B"
                                data={selectedGroup.teams
                                    ?.filter(t => t.participationId !== teamAId)
                                    .map(t => ({ value: t.participationId?.toString() || '', label: t.name }))}
                                value={teamBId?.toString()}
                                onChange={(val) => setTeamBId(val ? parseInt(val) : null)}
                                searchable
                            />
                        </Group>

                        {isDrilling ? (
                            <Center h={400}><Loader /></Center>
                        ) : drilledSolves?.details && drilledSolves.details.length > 0 ? (
                            <ScrollArea h={400}>
                                <Table striped highlightOnHover miw="46rem">
                                    <Table.Thead>
                                        <Table.Tr>
                                            <Table.Th w="14rem" miw="14rem">Challenge</Table.Th>
                                            <Table.Th w="11rem" miw="11rem">Time A</Table.Th>
                                            <Table.Th w="11rem" miw="11rem">Time B</Table.Th>
                                            <Table.Th w="8rem" miw="8rem">Diff</Table.Th>
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>
                                        {drilledSolves.details.map((solve: SequenceSuspectDetail, idx: number) => (
                                            <Table.Tr key={idx}>
                                                <Table.Td fw={500} miw="14rem">
                                                    <ScrollingText text={solve.challengeName || 'Unknown'} size="sm" maw={200} />
                                                </Table.Td>
                                                <Table.Td ff="monospace" fz="sm" miw="11rem">
                                                    {solve.timeA ? dayjs(solve.timeA).locale(locale).format('MM-DD HH:mm:ss') : '-'}
                                                </Table.Td>
                                                <Table.Td ff="monospace" fz="sm" miw="11rem">
                                                    {solve.timeB ? dayjs(solve.timeB).locale(locale).format('MM-DD HH:mm:ss') : '-'}
                                                </Table.Td>
                                                <Table.Td miw="8rem">
                                                    <Badge color={(solve.timeDiff ?? 0) < 60 ? 'red' : (solve.timeDiff ?? 0) < 300 ? 'yellow' : 'gray'}>
                                                        {(solve.timeDiff ?? 0).toFixed(0)}s
                                                    </Badge>
                                                </Table.Td>
                                            </Table.Tr>
                                        ))}
                                    </Table.Tbody>
                                </Table>
                            </ScrollArea>
                        ) : (
                            <Card withBorder padding="sm">
                                <ReadableDetails details={selectedGroup.details} />
                                <Text size="xs" c="dimmed" mt="xs">
                                    Common Solves: {selectedGroup.commonSolves?.join(', ')}
                                </Text>
                            </Card>
                        )}
                    </Stack>
                )}
            </Modal>

            <Modal
                opened={susOpened}
                onClose={closeSus}
                title={
                    <Group gap="xs">
                        <ThemeIcon size="sm" color="red" variant="light" radius="sm">
                            <Icon path={mdiShieldAlert} size={0.7} />
                        </ThemeIcon>
                        <Text fw={700}>Suspicion Details</Text>
                    </Group>
                }
                size="lg"
                centered
            >
                {selectedSuspicion && (
                    <Stack>
                        <Group justify="space-between" align="flex-start">
                            <Box>
                                <Text fw={700} size="lg">{selectedSuspicion.teamName}</Text>
                                <Text size="xs" c="dimmed">Suspicion score breakdown</Text>
                            </Box>
                            <Stack gap={4} align="center">
                                <RingProgress
                                    size={80}
                                    thickness={8}
                                    roundCaps
                                    sections={[{
                                        value: Math.min(selectedSuspicion.score ?? 0, 100),
                                        color: (selectedSuspicion.score ?? 0) >= 70 ? 'red' : 'yellow',
                                    }]}
                                    label={
                                        <Text ta="center" fw={900} size="sm" c={(selectedSuspicion.score ?? 0) >= 70 ? 'red' : 'yellow'}>
                                            {selectedSuspicion.score}
                                        </Text>
                                    }
                                />
                                <Text size="xs" c="dimmed">Risk Score</Text>
                            </Stack>
                        </Group>
                        <Divider />
                        <ScrollArea h={400}>
                            <Table striped miw="54rem">
                                <Table.Thead>
                                    <Table.Tr>
                                        <Table.Th w="10rem" miw="10rem">Type</Table.Th>
                                        <Table.Th w="10rem" miw="10rem">Score Delta</Table.Th>
                                        <Table.Th w="11rem" miw="11rem">Time</Table.Th>
                                        <Table.Th w="23rem" miw="23rem">Details</Table.Th>
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {selectedSuspicion.events?.map((evt, idx) => (
                                        <Table.Tr key={idx}>
                                            <Table.Td miw="10rem">
                                                <Badge color={evt.type === 'Corroboration' ? 'grape' : 'blue'}>{evt.type}</Badge>
                                            </Table.Td>
                                            <Table.Td miw="10rem">+{evt.scoreDelta}</Table.Td>
                                            <Table.Td fz="xs" ff="monospace" miw="11rem">
                                                {evt.time ? dayjs(evt.time).locale(locale).format('MM-DD HH:mm:ss') : '-'}
                                            </Table.Td>
                                            <Table.Td miw="23rem"><ReadableDetails details={evt.details} /></Table.Td>
                                        </Table.Tr>
                                    ))}
                                </Table.Tbody>
                            </Table>
                        </ScrollArea>
                    </Stack>
                )}
            </Modal>

            <SimpleGrid cols={{ base: 1, sm: 2, md: 4 }} spacing="md">
                {/* ── High Risk Teams card ── */}
                <UnstyledButton onClick={() => setActiveTab('suspicion')}>
                    <Card
                        shadow="sm"
                        padding="md"
                        radius="md"
                        withBorder
                        className={`${classes.summaryCard} ${activeTab === 'suspicion' ? classes.summaryCardActive : ''}`}
                    >
                        <Group justify="space-between" mb={6}>
                            <Text fw={600} size="sm" c="dimmed" tt="uppercase" style={{ letterSpacing: '0.04em' }}>High Risk Teams</Text>
                            <ThemeIcon
                                size="md"
                                radius="sm"
                                variant="gradient"
                                gradient={{ from: 'red.7', to: 'orange.5', deg: 135 }}
                            >
                                <Icon path={mdiShieldAlert} size={0.7} />
                            </ThemeIcon>
                        </Group>
                        <Title order={2} c="red" lh={1}>
                            {report?.suspicionList?.filter((x: any) => (x.score ?? 0) >= 70).length ?? 0}
                        </Title>
                        <Text size="xs" c="dimmed" mt={4}>of {report?.suspicionList?.length ?? 0} total teams</Text>
                        <Box className={classes.scoreBar} mt={8}>
                            <Box
                                className={classes.scoreBarFill}
                                style={{
                                    width: `${report?.suspicionList?.length
                                        ? (((report.suspicionList.filter((x: any) => (x.score ?? 0) >= 70).length) / report.suspicionList.length) * 100)
                                        : 0}%`,
                                    backgroundColor: 'var(--mantine-color-red-5)',
                                }}
                            />
                        </Box>
                    </Card>
                </UnstyledButton>

                {/* ── IP Anomalies card ── */}
                <UnstyledButton onClick={() => setActiveTab('ip')}>
                    <Card
                        shadow="sm"
                        padding="md"
                        radius="md"
                        withBorder
                        className={`${classes.summaryCard} ${activeTab === 'ip' ? classes.summaryCardActive : ''}`}
                    >
                        <Group justify="space-between" mb={6}>
                            <Text fw={600} size="sm" c="dimmed" tt="uppercase" style={{ letterSpacing: '0.04em' }}>IP Anomalies</Text>
                            <ThemeIcon
                                size="md"
                                radius="sm"
                                variant="gradient"
                                gradient={{ from: 'blue.7', to: 'cyan.4', deg: 135 }}
                            >
                                <Icon path={mdiIpNetwork} size={0.7} />
                            </ThemeIcon>
                        </Group>
                        <Title order={2} lh={1}>{report?.ipAnalysis?.length ?? 0}</Title>
                        <Text size="xs" c="dimmed" mt={4}>Suspicious IP activities</Text>
                        <Box className={classes.scoreBar} mt={8}>
                            <Box
                                className={classes.scoreBarFill}
                                style={{ width: `${Math.min((report?.ipAnalysis?.length ?? 0) * 8, 100)}%`, backgroundColor: 'var(--mantine-color-blue-5)' }}
                            />
                        </Box>
                    </Card>
                </UnstyledButton>

                {/* ── Abnormal Solves card ── */}
                <UnstyledButton onClick={() => setActiveTab('solve')}>
                    <Card
                        shadow="sm"
                        padding="md"
                        radius="md"
                        withBorder
                        className={`${classes.summaryCard} ${activeTab === 'solve' ? classes.summaryCardActive : ''}`}
                    >
                        <Group justify="space-between" mb={6}>
                            <Text fw={600} size="sm" c="dimmed" tt="uppercase" style={{ letterSpacing: '0.04em' }}>Abnormal Solves</Text>
                            <ThemeIcon
                                size="md"
                                radius="sm"
                                variant="gradient"
                                gradient={{ from: 'orange.6', to: 'yellow.4', deg: 135 }}
                            >
                                <Icon path={mdiGhost} size={0.7} />
                            </ThemeIcon>
                        </Group>
                        <Title order={2} lh={1}>{report?.abnormalSolves?.length ?? 0}</Title>
                        <Text size="xs" c="dimmed" mt={4}>Solves without prerequisites</Text>
                        <Box className={classes.scoreBar} mt={8}>
                            <Box
                                className={classes.scoreBarFill}
                                style={{ width: `${Math.min((report?.abnormalSolves?.length ?? 0) * 10, 100)}%`, backgroundColor: 'var(--mantine-color-orange-5)' }}
                            />
                        </Box>
                    </Card>
                </UnstyledButton>

                {/* ── Collusion Groups card ── */}
                <UnstyledButton onClick={() => setActiveTab('collusion')}>
                    <Card
                        shadow="sm"
                        padding="md"
                        radius="md"
                        withBorder
                        className={`${classes.summaryCard} ${activeTab === 'collusion' ? classes.summaryCardActive : ''}`}
                    >
                        <Group justify="space-between" mb={6}>
                            <Text fw={600} size="sm" c="dimmed" tt="uppercase" style={{ letterSpacing: '0.04em' }}>Collusion Groups</Text>
                            <ThemeIcon
                                size="md"
                                radius="sm"
                                variant="gradient"
                                gradient={{ from: 'grape.7', to: 'pink.4', deg: 135 }}
                            >
                                <Icon path={mdiAccountGroup} size={0.7} />
                            </ThemeIcon>
                        </Group>
                        <Title order={2} lh={1}>{report?.collusionGroups?.length ?? 0}</Title>
                        <Text size="xs" c="dimmed" mt={4}>High confidence rings</Text>
                        <Box className={classes.scoreBar} mt={8}>
                            <Box
                                className={classes.scoreBarFill}
                                style={{ width: `${Math.min((report?.collusionGroups?.length ?? 0) * 12, 100)}%`, backgroundColor: 'var(--mantine-color-grape-5)' }}
                            />
                        </Box>
                    </Card>
                </UnstyledButton>
            </SimpleGrid>

            <Paper shadow="md" p="md" radius="md">
                <Tabs value={activeTab} onChange={setActiveTab} variant="pills" radius="sm">
                    <Tabs.List grow className={classes.innerTabList} pb="xs" mb="xs">
                        <Tabs.Tab
                            value="suspicion"
                            leftSection={<Icon path={mdiShieldAlert} size={0.75} />}
                            rightSection={
                                <Badge size="xs" variant="filled" color={(report?.suspicionList?.filter((x: any) => (x.score ?? 0) >= 70).length ?? 0) > 0 ? 'red' : 'gray'} circle>
                                    {report?.suspicionList?.length ?? 0}
                                </Badge>
                            }
                        >
                            Suspicion
                        </Tabs.Tab>
                        <Tabs.Tab
                            value="ip"
                            leftSection={<Icon path={mdiIpNetwork} size={0.75} />}
                            rightSection={
                                <Badge size="xs" variant="filled" color={(report?.ipAnalysis?.length ?? 0) > 0 ? 'blue' : 'gray'} circle>
                                    {report?.ipAnalysis?.length ?? 0}
                                </Badge>
                            }
                        >
                            IP Analysis
                        </Tabs.Tab>
                        <Tabs.Tab
                            value="solve"
                            leftSection={<Icon path={mdiGhost} size={0.75} />}
                            rightSection={
                                <Badge size="xs" variant="filled" color={(report?.abnormalSolves?.length ?? 0) > 0 ? 'orange' : 'gray'} circle>
                                    {report?.abnormalSolves?.length ?? 0}
                                </Badge>
                            }
                        >
                            Abnormal Solves
                        </Tabs.Tab>
                        <Tabs.Tab
                            value="collusion"
                            leftSection={<Icon path={mdiAccountGroup} size={0.75} />}
                            rightSection={
                                <Badge size="xs" variant="filled" color={(report?.collusionGroups?.length ?? 0) > 0 ? 'grape' : 'gray'} circle>
                                    {report?.collusionGroups?.length ?? 0}
                                </Badge>
                            }
                        >
                            Collusion
                        </Tabs.Tab>
                    </Tabs.List>

                    <Tabs.Panel value="suspicion" pt="md">
                        <Group justify="space-between" mb="md">
                            <Group gap="xs">
                                <Title order={4}>Suspicion Rankings</Title>
                                <Badge variant="light" color="red">
                                    {sortedSuspicionList.length}
                                    {suspSearch && report?.suspicionList?.length !== sortedSuspicionList.length && (
                                        <> / {report?.suspicionList?.length ?? 0}</>
                                    )}
                                </Badge>
                            </Group>
                            <TextInput
                                placeholder="Search team name..."
                                leftSection={<Icon path={mdiMagnify} size={0.8} />}
                                value={suspSearch}
                                onChange={(e) => setSuspSearch(e.currentTarget.value)}
                                size="xs"
                                w={250}
                            />
                        </Group>
                        {report?.suspicionList && report.suspicionList.length > 0 ? (
                            <ScrollArea offsetScrollbars h={compactHeight}>
                                <Table
                                    className={tableClasses.table}
                                    horizontalSpacing="md"
                                    verticalSpacing="xs"
                                    striped
                                    highlightOnHover
                                    withTableBorder
                                    stickyHeader
                                    miw="46rem"
                                >
                                    <Table.Thead>
                                        <Table.Tr>
                                            <Table.Th w="3rem" miw="3rem" style={{ textAlign: 'center' }}>#</Table.Th>
                                            <ThSort sorted={suspSort.key === 'teamName'} reversed={suspSort.direction === 'desc'} onSort={() => handleSort(setSuspSort, suspSort, 'teamName')} w="16rem">Team</ThSort>
                                            <ThSort sorted={suspSort.key === 'score'} reversed={suspSort.direction === 'desc'} onSort={() => handleSort(setSuspSort, suspSort, 'score')} w="9rem">Score</ThSort>
                                            <Table.Th w="11rem" miw="11rem">Status</Table.Th>
                                            <Table.Th w="4rem" miw="4rem" style={{ textAlign: 'center' }}>View</Table.Th>
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>
                                        {sortedSuspicionList.map((item: any, index: number) => {
                                            const score = item.score ?? 0
                                            const currentStatus = item.status ?? ParticipationStatus.Pending
                                            const statusMeta = statusMap.get(currentStatus)
                                            const riskColor = score >= 500 ? 'red.9' : score >= 100 ? 'red' : score >= 70 ? 'orange' : 'yellow'

                                            const handleStatusChange = async (status: ParticipationStatus) => {
                                                try {
                                                    await api.admin.adminParticipation(item.participationId!, { status })
                                                    showNotification({ title: t('common.notify.success'), message: t('common.notify.updated'), color: 'green' })
                                                    mutate?.()
                                                } catch (e: any) {
                                                    showErrorMsg(e, t)
                                                }
                                            }

                                            return (
                                                <Table.Tr key={index}>
                                                    <Table.Td style={{ textAlign: 'center' }}>
                                                        <Text size="xs" c="dimmed" fw={600}>#{index + 1}</Text>
                                                    </Table.Td>
                                                    <Table.Td miw="14rem" style={{ maxWidth: '18rem', overflow: 'hidden' }}>
                                                        <Tooltip label={item.teamName || 'Unknown'} withArrow disabled={(item.teamName || '').length <= 24} multiline maw={280}>
                                                            <Text size="sm" fw={700} style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                                                {item.teamName || 'Unknown'}
                                                            </Text>
                                                        </Tooltip>
                                                    </Table.Td>
                                                    <Table.Td miw="9rem">
                                                        <Tooltip label={`Risk score: ${score}`} withArrow>
                                                            <Badge
                                                                color={riskColor}
                                                                size="md"
                                                                variant="filled"
                                                                leftSection={<Icon path={mdiAlertCircle} size={0.5} />}
                                                                style={{ fontVariantNumeric: 'tabular-nums', minWidth: '4rem', textAlign: 'center' }}
                                                            >
                                                                {score.toLocaleString()}
                                                            </Badge>
                                                        </Tooltip>
                                                    </Table.Td>
                                                    <Table.Td miw="11rem">
                                                        <Menu shadow="md" width={200}>
                                                            <Menu.Target>
                                                                <UnstyledButton style={{ cursor: 'pointer' }}>
                                                                    <Badge size="sm" color={statusMeta?.color || 'gray'} variant="light" rightSection={<Icon path={mdiChevronDown} size={0.55} />}>
                                                                        {statusMeta?.title || 'Unknown'}
                                                                    </Badge>
                                                                </UnstyledButton>
                                                            </Menu.Target>
                                                            <Menu.Dropdown>
                                                                <Menu.Label>{t('common.label.status')}</Menu.Label>
                                                                {Array.from(statusMap.entries())
                                                                    .filter(([status]) => status === ParticipationStatus.Accepted || status === ParticipationStatus.Suspended)
                                                                    .map(([status, meta]) => (
                                                                        <Menu.Item key={status} leftSection={<Icon path={meta.iconPath} size={0.8} color={meta.color === 'alert' ? 'red' : meta.color} />} onClick={() => handleStatusChange(status)} disabled={currentStatus === status}>
                                                                            {meta.title}
                                                                        </Menu.Item>
                                                                    ))}
                                                            </Menu.Dropdown>
                                                        </Menu>
                                                    </Table.Td>
                                                    <Table.Td style={{ textAlign: 'center' }}>
                                                        <Tooltip label="View suspicion details" withArrow>
                                                            <ActionIcon variant="subtle" color="blue" size="sm" onClick={() => handleViewSuspicion(item)}>
                                                                <Icon path={mdiOpenInNew} size={0.7} />
                                                            </ActionIcon>
                                                        </Tooltip>
                                                    </Table.Td>
                                                </Table.Tr>
                                            )
                                        })}
                                    </Table.Tbody>
                                </Table>
                            </ScrollArea>
                        ) : (
                            <Center className={classes.emptyState} py="xl">
                                <Stack align="center" gap="xs">
                                    <ThemeIcon size={48} radius="xl" color="green" variant="light">
                                        <Icon path={mdiCheckCircle} size={1.4} />
                                    </ThemeIcon>
                                    <Text fw={600} size="md">All Clear</Text>
                                    <Text size="sm" c="dimmed">No suspicion scores recorded</Text>
                                </Stack>
                            </Center>
                        )}
                    </Tabs.Panel>

                    <Tabs.Panel value="ip" pt="md">
                        <Group justify="space-between" mb="md">
                            <Group gap="xs">
                                <Title order={4}>IP Analysis</Title>
                                <Badge variant="light" color="blue">
                                    {report?.ipAnalysis?.length ?? 0}
                                </Badge>
                            </Group>
                            <TextInput placeholder="Search team, user, IP, or details..." leftSection={<Icon path={mdiMagnify} size={0.8} />} value={ipSearch} onChange={(e) => setIpSearch(e.currentTarget.value)} size="xs" w={250} />
                        </Group>
                        {report?.ipAnalysis && report.ipAnalysis.length > 0 ? (
                            <ScrollArea offsetScrollbars h={roomyHeight}>
                                <Table
                                    className={tableClasses.table}
                                    horizontalSpacing="md"
                                    verticalSpacing="xs"
                                    striped
                                    highlightOnHover
                                    withTableBorder
                                    stickyHeader
                                    miw="76rem"
                                >
                                    <Table.Thead>
                                        <Table.Tr>
                                            <ThSort sorted={ipSort.key === 'teamName'} reversed={ipSort.direction === 'desc'} onSort={() => handleSort(setIpSort, ipSort, 'teamName')} w="11rem">{t('common.label.team', 'Team')}</ThSort>
                                            <ThSort sorted={ipSort.key === 'type'} reversed={ipSort.direction === 'desc'} onSort={() => handleSort(setIpSort, ipSort, 'type')} w="11rem">Type</ThSort>
                                            <Table.Th w="14rem" miw="14rem">Users</Table.Th>
                                            <ThSort sorted={ipSort.key === 'ip'} reversed={ipSort.direction === 'desc'} onSort={() => handleSort(setIpSort, ipSort, 'ip')} w="9rem">IP</ThSort>
                                            <ThSort sorted={ipSort.key === 'time'} reversed={ipSort.direction === 'desc'} onSort={() => handleSort(setIpSort, ipSort, 'time')} w="9rem">Time</ThSort>
                                            <Table.Th>Details</Table.Th>
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>
                                        {sortedIpAnalysis.map((item: any, index: number) => {
                                            const meta = IP_TYPE_META[item.type] ?? { label: 'Unknown', color: 'grape' }
                                            const absTime = item.time ? dayjs(item.time).locale(locale).format('YYYY-MM-DD HH:mm:ss') : '-'
                                            const relTime = item.time ? dayjs(item.time).fromNow() : '-'
                                            return (
                                                <Table.Tr key={index}>
                                                    <Table.Td miw="10rem" style={{ maxWidth: '14rem', overflow: 'hidden' }}>
                                                        <Tooltip label={item.teamName || 'Unknown'} withArrow disabled={(item.teamName || '').length <= 20} multiline maw={240}>
                                                            <Text size="sm" fw={700} style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                                                {item.teamName || 'Unknown'}
                                                            </Text>
                                                        </Tooltip>
                                                    </Table.Td>
                                                    <Table.Td w="10rem" miw="10rem">
                                                        <Badge
                                                            color={meta.color}
                                                            size="xs"
                                                            variant="light"
                                                            leftSection={<Icon path={meta.icon} size={0.45} />}
                                                        >
                                                            {meta.label}
                                                        </Badge>
                                                    </Table.Td>
                                                    <Table.Td miw="12rem">
                                                        <UsersCell users={item.userNames} relatedUsers={item.relatedUsers} />
                                                    </Table.Td>
                                                    <Table.Td miw="9rem" style={{ maxWidth: '14rem', overflow: 'hidden' }}>
                                                        <Group gap={4} wrap="nowrap">
                                                            <Tooltip label={item.ip || '-'} withArrow disabled={!item.ip || item.ip.length <= 20} multiline maw={360}>
                                                                <Text ff="monospace" fz="xs" style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', flex: 1 }}>
                                                                    {item.ip || '-'}
                                                                </Text>
                                                            </Tooltip>
                                                            {item.ip && <CopyButton value={item.ip} />}
                                                        </Group>
                                                    </Table.Td>
                                                    <Table.Td miw="9rem">
                                                        <Tooltip label={absTime} withArrow>
                                                            <Group gap={4} wrap="nowrap" style={{ cursor: 'default' }}>
                                                                <Icon path={mdiClockOutline} size={0.6} color="var(--mantine-color-dimmed)" />
                                                                <Text fz="xs" c="dimmed">{relTime}</Text>
                                                            </Group>
                                                        </Tooltip>
                                                    </Table.Td>
                                                    <Table.Td style={{ maxWidth: '28rem', overflow: 'hidden' }}><ReadableDetails details={item.details} maxRows={3} /></Table.Td>
                                                </Table.Tr>
                                            )
                                        })}
                                    </Table.Tbody>
                                </Table>
                            </ScrollArea>
                        ) : (
                            <Center className={classes.emptyState} py="xl">
                                <Stack align="center" gap="xs">
                                    <ThemeIcon size={48} radius="xl" color="green" variant="light">
                                        <Icon path={mdiCheckCircle} size={1.4} />
                                    </ThemeIcon>
                                    <Text fw={600} size="md">All Clear</Text>
                                    <Text size="sm" c="dimmed">{t('game.content.no_cheat.title', 'No IP anomalies detected')}</Text>
                                </Stack>
                            </Center>
                        )}
                    </Tabs.Panel>

                    <Tabs.Panel value="solve" pt="md">
                        <Group justify="space-between" mb="md">
                            <Group gap="xs">
                                <Title order={4}>Abnormal Solves</Title>
                                <Badge variant="light" color="orange">
                                    {report?.abnormalSolves?.length ?? 0}
                                </Badge>
                            </Group>
                            <TextInput placeholder="Search team, challenge, or type..." leftSection={<Icon path={mdiMagnify} size={0.8} />} value={solveSearch} onChange={(e) => setSolveSearch(e.currentTarget.value)} size="xs" w={250} />
                        </Group>
                        {report?.abnormalSolves && report.abnormalSolves.length > 0 ? (
                            <ScrollArea offsetScrollbars h={roomyHeight}>
                                <Table
                                    className={tableClasses.table}
                                    horizontalSpacing="md"
                                    verticalSpacing="xs"
                                    striped
                                    highlightOnHover
                                    withTableBorder
                                    stickyHeader
                                    miw="68rem"
                                >
                                    <Table.Thead>
                                        <Table.Tr>
                                            <ThSort sorted={solveSort.key === 'teamName'} reversed={solveSort.direction === 'desc'} onSort={() => handleSort(setSolveSort, solveSort, 'teamName')} w="11rem">{t('common.label.team', 'Team')}</ThSort>
                                            <ThSort sorted={solveSort.key === 'challengeName'} reversed={solveSort.direction === 'desc'} onSort={() => handleSort(setSolveSort, solveSort, 'challengeName')} w="13rem">{t('common.label.challenge', 'Challenge')}</ThSort>
                                            <ThSort sorted={solveSort.key === 'type'} reversed={solveSort.direction === 'desc'} onSort={() => handleSort(setSolveSort, solveSort, 'type')} w="9rem">Type</ThSort>
                                            <Table.Th>Details</Table.Th>
                                            <ThSort sorted={solveSort.key === 'solveTime'} reversed={solveSort.direction === 'desc'} onSort={() => handleSort(setSolveSort, solveSort, 'solveTime')} w="9rem">{t('common.label.time', 'Time')}</ThSort>
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>
                                        {sortedAbnormalSolves.map((item: any, index: number) => {
                                            const typeColor = item.type === 'Hoarding' ? 'cyan' : item.type === 'NoDownload' ? 'violet' : item.type === 'NoContainer' ? 'indigo' : 'orange'
                                            const typeIcon = item.type === 'NoDownload' ? mdiDownload : item.type === 'NoContainer' ? mdiCubeOutline : mdiGhost
                                            const typeLabel = item.type === 'NoDownload' ? 'No Download' : item.type === 'NoContainer' ? 'No Container' : item.type
                                            const absTime = dayjs(item.solveTime).locale(locale).format('YYYY-MM-DD HH:mm:ss')
                                            const relTime = dayjs(item.solveTime).fromNow()
                                            return (
                                                <Table.Tr key={index}>
                                                    <Table.Td miw="10rem" style={{ maxWidth: '14rem', overflow: 'hidden' }}>
                                                        <Tooltip label={item.teamName || 'Unknown'} withArrow disabled={(item.teamName || '').length <= 20} multiline maw={240}>
                                                            <Text size="sm" fw={700} style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                                                {item.teamName || 'Unknown'}
                                                            </Text>
                                                        </Tooltip>
                                                    </Table.Td>
                                                    <Table.Td miw="12rem" style={{ maxWidth: '16rem', overflow: 'hidden' }}>
                                                        <Tooltip label={item.challengeName || 'Unknown'} withArrow disabled={(item.challengeName || '').length <= 22} multiline maw={240}>
                                                            <Text size="sm" style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                                                {item.challengeName || 'Unknown'}
                                                            </Text>
                                                        </Tooltip>
                                                    </Table.Td>
                                                    <Table.Td miw="9rem">
                                                        <Badge
                                                            color={typeColor}
                                                            size="xs"
                                                            variant="light"
                                                            leftSection={<Icon path={typeIcon} size={0.5} />}
                                                        >
                                                            {typeLabel}
                                                        </Badge>
                                                    </Table.Td>
                                                    <Table.Td style={{ maxWidth: '28rem', overflow: 'hidden' }}><ReadableDetails details={item.details} maxRows={3} /></Table.Td>
                                                    <Table.Td miw="9rem">
                                                        <Tooltip label={absTime} withArrow>
                                                            <Group gap={4} wrap="nowrap" style={{ cursor: 'default' }}>
                                                                <Icon path={mdiClockOutline} size={0.6} color="var(--mantine-color-dimmed)" />
                                                                <Text fz="xs" c="dimmed">{relTime}</Text>
                                                            </Group>
                                                        </Tooltip>
                                                    </Table.Td>
                                                </Table.Tr>
                                            )
                                        })}
                                    </Table.Tbody>
                                </Table>
                            </ScrollArea>
                        ) : (
                            <Center className={classes.emptyState} py="xl">
                                <Stack align="center" gap="xs">
                                    <ThemeIcon size={48} radius="xl" color="green" variant="light">
                                        <Icon path={mdiCheckCircle} size={1.4} />
                                    </ThemeIcon>
                                    <Text fw={600} size="md">All Clear</Text>
                                    <Text size="sm" c="dimmed">{t('game.content.no_cheat.comment', 'No abnormal solves detected')}</Text>
                                </Stack>
                            </Center>
                        )}
                    </Tabs.Panel>

                    <Tabs.Panel value="collusion" pt="md">
                        <Group justify="space-between" mb="md">
                            <Group gap="xs">
                                <Title order={4}>Collusion Groups</Title>
                                <Badge variant="light" color="grape">
                                    {report?.collusionGroups?.length ?? 0}
                                </Badge>
                            </Group>
                            <TextInput placeholder="Search team name..." leftSection={<Icon path={mdiMagnify} size={0.8} />} value={collusionSearch} onChange={(e) => setCollusionSearch(e.currentTarget.value)} size="xs" w={250} />
                        </Group>
                        {report?.collusionGroups && report.collusionGroups.length > 0 ? (
                            <ScrollArea offsetScrollbars h={roomyHeight}>
                                <Table
                                    className={tableClasses.table}
                                    horizontalSpacing="md"
                                    verticalSpacing="xs"
                                    striped
                                    highlightOnHover
                                    withTableBorder
                                    stickyHeader
                                    miw="72rem"
                                >
                                    <Table.Thead>
                                        <Table.Tr>
                                            <Table.Th w="18rem">Teams</Table.Th>
                                            <ThSort sorted={collusionSort.key === 'averageRsi'} reversed={collusionSort.direction === 'desc'} onSort={() => handleSort(setCollusionSort, collusionSort, 'averageRsi')} w="12rem">Similarity</ThSort>
                                            <Table.Th w="14rem" miw="14rem">Common Solves</Table.Th>
                                            <Table.Th>Details</Table.Th>
                                            <Table.Th w="4rem" miw="4rem" style={{ textAlign: 'center' }}>View</Table.Th>
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>
                                        {sortedCollusionGroups.map((item: any, index: number) => {
                                            const rsi = item.averageRsi ?? 0
                                            const rsiPct = +(rsi * 100).toFixed(1)
                                            const rsiColor = rsi > 0.9 ? 'red' : rsi > 0.8 ? 'orange' : 'yellow'
                                            const commonCount = item.commonSolves?.length ?? 0
                                            return (
                                                <Table.Tr key={index}>
                                                    <Table.Td miw="16rem" style={{ maxWidth: '20rem', overflow: 'hidden' }}>
                                                        <Stack gap={3}>
                                                            {item.teams?.map((team: CollusionTeamInfo, idx: number) => (
                                                                <Group key={idx} gap={6} wrap="nowrap" style={{ minWidth: 0 }}>
                                                                    <Badge size="xs" variant="dot" color={idx === 0 ? 'blue' : 'grape'} />
                                                                    <Tooltip label={team.name} withArrow disabled={(team.name || '').length <= 24} multiline maw={280}>
                                                                        <Text size="sm" fw={600} style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                                                                            {team.name}
                                                                        </Text>
                                                                    </Tooltip>
                                                                </Group>
                                                            ))}
                                                        </Stack>
                                                    </Table.Td>
                                                    <Table.Td miw="12rem">
                                                        <Stack gap={3}>
                                                            <Group gap={6} justify="space-between">
                                                                <Text fz="xs" fw={700} c={rsiColor}>{rsiPct}%</Text>
                                                                <Badge size="xs" color={rsiColor} variant="light">
                                                                    {rsi > 0.9 ? 'Critical' : rsi > 0.8 ? 'High' : 'Medium'}
                                                                </Badge>
                                                            </Group>
                                                            <Progress value={rsiPct} color={rsiColor} size="xs" radius="xs" />
                                                        </Stack>
                                                    </Table.Td>
                                                    <Table.Td miw="14rem">
                                                        {commonCount === 0 ? (
                                                            <Text fz="xs" c="dimmed">—</Text>
                                                        ) : (
                                                            <Group gap={4} wrap="wrap">
                                                                {item.commonSolves?.slice(0, 3).map((s: string, i: number) => (
                                                                    <Badge key={i} size="xs" variant="light" color="grape">{s}</Badge>
                                                                ))}
                                                                {commonCount > 3 && (
                                                                    <Popover width={300} position="top" withArrow shadow="md" withinPortal>
                                                                        <Popover.Target>
                                                                            <Badge size="xs" variant="outline" color="grape" style={{ cursor: 'pointer' }}>
                                                                                +{commonCount - 3} more
                                                                            </Badge>
                                                                        </Popover.Target>
                                                                        <Popover.Dropdown>
                                                                            <Text size="xs" fw={700} c="dimmed" mb={6}>All {commonCount} Common Challenges</Text>
                                                                            <Group gap={4} wrap="wrap">
                                                                                {item.commonSolves?.map((s: string, i: number) => (
                                                                                    <Badge key={i} size="xs" variant="light" color="grape">{s}</Badge>
                                                                                ))}
                                                                            </Group>
                                                                        </Popover.Dropdown>
                                                                    </Popover>
                                                                )}
                                                            </Group>
                                                        )}
                                                    </Table.Td>
                                                    <Table.Td style={{ maxWidth: '28rem', overflow: 'hidden' }}><ReadableDetails details={item.details} maxRows={3} /></Table.Td>
                                                    <Table.Td style={{ textAlign: 'center' }}>
                                                        <Tooltip label="View collusion details" withArrow>
                                                            <ActionIcon variant="subtle" color="grape" size="sm" onClick={() => handleViewDetails(item)}>
                                                                <Icon path={mdiOpenInNew} size={0.7} />
                                                            </ActionIcon>
                                                        </Tooltip>
                                                    </Table.Td>
                                                </Table.Tr>
                                            )
                                        })}
                                    </Table.Tbody>
                                </Table>
                            </ScrollArea>
                        ) : (
                            <Center className={classes.emptyState} py="xl">
                                <Stack align="center" gap="xs">
                                    <ThemeIcon size={48} radius="xl" color="green" variant="light">
                                        <Icon path={mdiCheckCircle} size={1.4} />
                                    </ThemeIcon>
                                    <Text fw={600} size="md">All Clear</Text>
                                    <Text size="sm" c="dimmed">No collusion groups detected</Text>
                                </Stack>
                            </Center>
                        )}
                    </Tabs.Panel>
                </Tabs>
            </Paper>
        </>
    )
}

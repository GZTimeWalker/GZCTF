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
} from '@mantine/core'
import { FC, useState, useMemo } from 'react'
import { Icon } from '@mdi/react'
import { mdiCheckCircle, mdiGhost, mdiIpNetwork, mdiArrowUp, mdiArrowDown, mdiUnfoldMoreHorizontal, mdiInformation, mdiMagnify, mdiShieldAlert, mdiChevronDown, mdiAccountGroup } from '@mdi/js'
import { useTranslation } from 'react-i18next'
import dayjs from 'dayjs'
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

const IP_TYPE_META: Record<string, { label: string; color: string }> = {
    SharedIP: { label: 'Shared IP', color: 'orange' },
    SharedFingerprint: { label: 'Shared Fingerprint', color: 'orange' },
    FingerprintChurn: { label: 'Fingerprint Churn', color: 'yellow' },
    IpChurn: { label: 'IP Churn', color: 'yellow' },
    CrossTeamIP: { label: 'Cross-Team IP', color: 'red' },
    TokenAbuse: { label: 'Token Abuse', color: 'red' },
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

const ReadableDetails: FC<{ details?: string | null; maxRows?: number }> = ({ details, maxRows }) => {
    const lines = parseDetailLines(details)
    if (lines.length === 0) return <Text size="sm">-</Text>
    const visibleLines = maxRows ? lines.slice(0, maxRows) : lines
    const remaining = lines.length - visibleLines.length

    return (
        <Stack gap={4} className={classes.detailsBox}>
            {visibleLines.map((line, idx) => (
                line.label ? (
                    <Group key={idx} gap={6} align="flex-start" wrap="nowrap">
                        <Text size="xs" fw={700} c="dimmed" style={{ minWidth: 92 }}>
                            {line.label}
                        </Text>
                        <Text size="sm">{line.value}</Text>
                    </Group>
                ) : (
                    <Text key={idx} size="sm">{line.value}</Text>
                )
            ))}
            {remaining > 0 && (
                <Text size="xs" c="dimmed">
                    +{remaining} more
                </Text>
            )}
        </Stack>
    )
}

const UsersCell: FC<{ users?: string[]; relatedUsers?: string[] }> = ({ users, relatedUsers }) => {
    const currentUsers = (users ?? []).filter(Boolean)
    const others = (relatedUsers ?? []).filter(Boolean)

    if (currentUsers.length === 0 && others.length === 0) {
        return <Text size="sm" c="dimmed">-</Text>
    }

    return (
        <Group gap={4} className={classes.userWrap}>
            {currentUsers.map((user) => (
                <Badge key={`own-${user}`} size="xs" color="blue" variant="light">
                    {user}
                </Badge>
            ))}
            {others.slice(0, 3).map((user) => (
                <Badge key={`other-${user}`} size="xs" color="gray" variant="light">
                    {user}
                </Badge>
            ))}
            {others.length > 3 && (
                <Badge size="xs" color="gray" variant="outline">
                    +{others.length - 3}
                </Badge>
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
        return sortData(report.suspicionList, suspSort)
    }, [report?.suspicionList, suspSort])

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
            <Modal opened={opened} onClose={close} title="Collusion Details" size="xl" centered>
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

            <Modal opened={susOpened} onClose={closeSus} title="Suspicion Details" size="lg" centered>
                {selectedSuspicion && (
                    <Stack>
                        <Group justify="space-between">
                            <Text fw={700} size="xl">{selectedSuspicion.teamName}</Text>
                            <Badge size="lg" color={(selectedSuspicion.score ?? 0) >= 100 ? 'gray' : (selectedSuspicion.score ?? 0) >= 70 ? 'red' : 'yellow'}>
                                Score: {selectedSuspicion.score}
                            </Badge>
                        </Group>
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

            <SimpleGrid cols={{ base: 1, md: 4 }} spacing="md">
                <UnstyledButton onClick={() => setActiveTab('suspicion')}>
                    <Card
                        shadow="sm"
                        padding="md"
                        radius="md"
                        withBorder
                        className={classes.summaryCard}
                        style={{ borderColor: activeTab === 'suspicion' ? 'var(--mantine-color-red-5)' : undefined }}
                    >
                        <Group justify="space-between" mb="xs">
                            <Text fw={500} c="red">High Risk Teams</Text>
                            <ThemeIcon color="red" variant="light">
                                <Icon path={mdiShieldAlert} size={0.8} />
                            </ThemeIcon>
                        </Group>
                        <Title order={3} c="red">
                            {report?.suspicionList?.filter((x: any) => (x.score ?? 0) >= 70).length ?? 0}
                        </Title>
                        <Text size="sm" c="dimmed">Score {'>'}= 70</Text>
                    </Card>
                </UnstyledButton>

                <UnstyledButton onClick={() => setActiveTab('ip')}>
                    <Card
                        shadow="sm"
                        padding="md"
                        radius="md"
                        withBorder
                        className={classes.summaryCard}
                        style={{ borderColor: activeTab === 'ip' ? 'var(--mantine-color-red-5)' : undefined }}
                    >
                        <Group justify="space-between" mb="xs">
                            <Text fw={500}>IP Anomalies</Text>
                            <ThemeIcon color="red" variant="light">
                                <Icon path={mdiIpNetwork} size={0.8} />
                            </ThemeIcon>
                        </Group>
                        <Title order={3}>{report?.ipAnalysis?.length ?? 0}</Title>
                        <Text size="sm" c="dimmed">Suspicious IP activities</Text>
                    </Card>
                </UnstyledButton>

                <UnstyledButton onClick={() => setActiveTab('solve')}>
                    <Card
                        shadow="sm"
                        padding="md"
                        radius="md"
                        withBorder
                        className={classes.summaryCard}
                        style={{ borderColor: activeTab === 'solve' ? 'var(--mantine-color-red-5)' : undefined }}
                    >
                        <Group justify="space-between" mb="xs">
                            <Text fw={500}>Abnormal Solves</Text>
                            <ThemeIcon color="orange" variant="light">
                                <Icon path={mdiGhost} size={0.8} />
                            </ThemeIcon>
                        </Group>
                        <Title order={3}>{report?.abnormalSolves?.length ?? 0}</Title>
                        <Text size="sm" c="dimmed">Solves without prerequisites</Text>
                    </Card>
                </UnstyledButton>

                <UnstyledButton onClick={() => setActiveTab('collusion')}>
                    <Card
                        shadow="sm"
                        padding="md"
                        radius="md"
                        withBorder
                        className={classes.summaryCard}
                        style={{ borderColor: activeTab === 'collusion' ? 'var(--mantine-color-red-5)' : undefined }}
                    >
                        <Group justify="space-between" mb="xs">
                            <Text fw={500}>Collusion Groups</Text>
                            <ThemeIcon color="red" variant="light">
                                <Icon path={mdiAccountGroup} size={0.8} />
                            </ThemeIcon>
                        </Group>
                        <Title order={3}>{report?.collusionGroups?.length ?? 0}</Title>
                        <Text size="sm" c="dimmed">High confidence rings</Text>
                    </Card>
                </UnstyledButton>
            </SimpleGrid>

            <Paper shadow="md" p="md">
                <Tabs value={activeTab} onChange={setActiveTab}>
                    <Tabs.List grow>
                        <Tabs.Tab value="suspicion">Suspicion ({report?.suspicionList?.length ?? 0})</Tabs.Tab>
                        <Tabs.Tab value="ip">IP Analysis ({report?.ipAnalysis?.length ?? 0})</Tabs.Tab>
                        <Tabs.Tab value="solve">Abnormal Solves ({report?.abnormalSolves?.length ?? 0})</Tabs.Tab>
                        <Tabs.Tab value="collusion">Collusion ({report?.collusionGroups?.length ?? 0})</Tabs.Tab>
                    </Tabs.List>

                    <Tabs.Panel value="suspicion" pt="md">
                        <Group justify="space-between" mb="md">
                            <Group gap="xs">
                                <Title order={4}>Suspicion Rankings</Title>
                                <Badge variant="light" color="red">
                                    {report?.suspicionList?.length ?? 0}
                                </Badge>
                            </Group>
                        </Group>
                        {report?.suspicionList && report.suspicionList.length > 0 ? (
                            <ScrollArea offsetScrollbars h={compactHeight}>
                                <Table
                                    className={tableClasses.table}
                                    horizontalSpacing="md"
                                    verticalSpacing="sm"
                                    striped
                                    highlightOnHover
                                    withTableBorder
                                    stickyHeader
                                    miw={suspicionTableMinWidth}
                                >
                                    <Table.Thead>
                                        <Table.Tr>
                                            <ThSort sorted={suspSort.key === 'teamName'} reversed={suspSort.direction === 'desc'} onSort={() => handleSort(setSuspSort, suspSort, 'teamName')} w="14rem">Team</ThSort>
                                            <ThSort sorted={suspSort.key === 'score'} reversed={suspSort.direction === 'desc'} onSort={() => handleSort(setSuspSort, suspSort, 'score')} w="8rem">Score</ThSort>
                                            <Table.Th w="12rem" miw="12rem">Status</Table.Th>
                                            <Table.Th w="9rem" miw="9rem">Actions</Table.Th>
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>
                                        {sortedSuspicionList.map((item: any, index: number) => {
                                            const score = item.score ?? 0
                                            const currentStatus = item.status ?? ParticipationStatus.Pending
                                            const statusMeta = statusMap.get(currentStatus)

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
                                                    <Table.Td fw={700} miw="14rem"><ScrollingText text={item.teamName || 'Unknown'} size="md" fw="bold" maw={260} /></Table.Td>
                                                    <Table.Td miw="8rem"><Badge color={score >= 70 ? 'red' : 'yellow'} size="lg" variant="light">{score}</Badge></Table.Td>
                                                    <Table.Td miw="12rem">
                                                        <Menu shadow="md" width={200}>
                                                            <Menu.Target>
                                                                <UnstyledButton style={{ cursor: 'pointer' }}>
                                                                    <Badge color={statusMeta?.color || 'gray'} rightSection={<Icon path={mdiChevronDown} size={0.6} />}>
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
                                                    <Table.Td miw="9rem">
                                                        <Button size="sm" variant="default" onClick={() => handleViewSuspicion(item)}>View Details</Button>
                                                    </Table.Td>
                                                </Table.Tr>
                                            )
                                        })}
                                    </Table.Tbody>
                                </Table>
                            </ScrollArea>
                        ) : (
                            <Alert color="green">No suspicion scores recorded</Alert>
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
                                    verticalSpacing="sm"
                                    striped
                                    highlightOnHover
                                    withTableBorder
                                    stickyHeader
                                    miw={ipTableMinWidth}
                                >
                                    <Table.Thead>
                                        <Table.Tr>
                                            <ThSort sorted={ipSort.key === 'teamName'} reversed={ipSort.direction === 'desc'} onSort={() => handleSort(setIpSort, ipSort, 'teamName')} w="11rem">{t('common.label.team', 'Team')}</ThSort>
                                            <ThSort sorted={ipSort.key === 'type'} reversed={ipSort.direction === 'desc'} onSort={() => handleSort(setIpSort, ipSort, 'type')} w="12rem">Type</ThSort>
                                            <Table.Th w="16rem" miw="16rem">Users</Table.Th>
                                            <ThSort sorted={ipSort.key === 'ip'} reversed={ipSort.direction === 'desc'} onSort={() => handleSort(setIpSort, ipSort, 'ip')} w="10rem">IP</ThSort>
                                            <ThSort sorted={ipSort.key === 'time'} reversed={ipSort.direction === 'desc'} onSort={() => handleSort(setIpSort, ipSort, 'time')} w="11rem">Time</ThSort>
                                            <Table.Th w="24rem" miw="24rem">Details</Table.Th>
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>
                                        {sortedIpAnalysis.map((item: any, index: number) => (
                                            <Table.Tr key={index}>
                                                <Table.Td miw="11rem"><ScrollingText text={item.teamName || 'Unknown'} size="md" fw="bold" maw={220} /></Table.Td>
                                                <Table.Td w="12rem" miw="12rem">
                                                    {(() => {
                                                        const meta = IP_TYPE_META[item.type] ?? { label: 'Unknown', color: 'grape' }
                                                        return <Badge color={meta.color} size="xs" fullWidth>{meta.label}</Badge>
                                                    })()}
                                                </Table.Td>
                                                <Table.Td miw="16rem">
                                                    <UsersCell users={item.userNames} relatedUsers={item.relatedUsers} />
                                                </Table.Td>
                                                <Table.Td ff="monospace" miw="10rem">{item.ip}</Table.Td>
                                                <Table.Td ff="monospace" fz="sm" miw="11rem">{item.time ? dayjs(item.time).locale(locale).format('MM-DD HH:mm:ss') : '-'}</Table.Td>
                                                <Table.Td miw="24rem"><ReadableDetails details={item.details} maxRows={4} /></Table.Td>
                                            </Table.Tr>
                                        ))}
                                    </Table.Tbody>
                                </Table>
                            </ScrollArea>
                        ) : (
                            <Alert color="green" icon={<Icon path={mdiCheckCircle} size={1} />}>{t('game.content.no_cheat.title', 'No IP anomalies detected')}</Alert>
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
                                    verticalSpacing="sm"
                                    striped
                                    highlightOnHover
                                    withTableBorder
                                    stickyHeader
                                    miw={solveTableMinWidth}
                                >
                                    <Table.Thead>
                                        <Table.Tr>
                                            <ThSort sorted={solveSort.key === 'teamName'} reversed={solveSort.direction === 'desc'} onSort={() => handleSort(setSolveSort, solveSort, 'teamName')} w="11rem">{t('common.label.team', 'Team')}</ThSort>
                                            <ThSort sorted={solveSort.key === 'challengeName'} reversed={solveSort.direction === 'desc'} onSort={() => handleSort(setSolveSort, solveSort, 'challengeName')} w="14rem">{t('common.label.challenge', 'Challenge')}</ThSort>
                                            <ThSort sorted={solveSort.key === 'type'} reversed={solveSort.direction === 'desc'} onSort={() => handleSort(setSolveSort, solveSort, 'type')} w="10rem">Type</ThSort>
                                            <Table.Th w="24rem" miw="24rem">Details</Table.Th>
                                            <ThSort sorted={solveSort.key === 'solveTime'} reversed={solveSort.direction === 'desc'} onSort={() => handleSort(setSolveSort, solveSort, 'solveTime')} w="12rem">{t('common.label.time', 'Time')}</ThSort>
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>
                                        {sortedAbnormalSolves.map((item: any, index: number) => (
                                            <Table.Tr key={index}>
                                                <Table.Td miw="11rem"><ScrollingText text={item.teamName || 'Unknown'} size="md" fw="bold" maw={220} /></Table.Td>
                                                <Table.Td miw="14rem"><ScrollingText text={item.challengeName || 'Unknown'} size="md" maw={260} /></Table.Td>
                                                <Table.Td miw="10rem">
                                                    <Badge color={item.type === 'Hoarding' ? 'cyan' : 'orange'} size="sm">
                                                        {item.type === 'NoDownload' ? 'No Download' : item.type === 'NoContainer' ? 'No Container' : item.type}
                                                    </Badge>
                                                </Table.Td>
                                                <Table.Td fz="sm" miw="24rem"><ReadableDetails details={item.details} maxRows={4} /></Table.Td>
                                                <Table.Td ff="monospace" miw="12rem"><Badge size="md" color="indigo">{dayjs(item.solveTime).locale(locale).format('MM-DD HH:mm:ss')}</Badge></Table.Td>
                                            </Table.Tr>
                                        ))}
                                    </Table.Tbody>
                                </Table>
                            </ScrollArea>
                        ) : (
                            <Alert color="green" icon={<Icon path={mdiCheckCircle} size={1} />}>{t('game.content.no_cheat.comment', 'No abnormal solves detected')}</Alert>
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
                                    verticalSpacing="sm"
                                    striped
                                    highlightOnHover
                                    withTableBorder
                                    stickyHeader
                                    miw={collusionTableMinWidth}
                                >
                                    <Table.Thead>
                                        <Table.Tr>
                                            <Table.Th w="20rem">Teams</Table.Th>
                                            <ThSort sorted={collusionSort.key === 'averageRsi'} reversed={collusionSort.direction === 'desc'} onSort={() => handleSort(setCollusionSort, collusionSort, 'averageRsi')} w="10rem">Avg RSI</ThSort>
                                            <Table.Th w="16rem" miw="16rem">Common Solves</Table.Th>
                                            <Table.Th w="24rem" miw="24rem">Details</Table.Th>
                                            <Table.Th w="9rem" miw="9rem">Action</Table.Th>
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>
                                        {sortedCollusionGroups.map((item: any, index: number) => (
                                            <Table.Tr key={index}>
                                                <Table.Td miw="20rem">
                                                    <Stack gap={2}>
                                                        {item.teams?.map((team: CollusionTeamInfo, idx: number) => (
                                                            <ScrollingText key={idx} text={team.name} size="md" fw="bold" maw={320} />
                                                        ))}
                                                    </Stack>
                                                </Table.Td>
                                                <Table.Td miw="10rem"><Badge color={(item.averageRsi ?? 0) > 0.8 ? 'red' : 'yellow'} size="sm">{((item.averageRsi ?? 0) * 100).toFixed(1)}%</Badge></Table.Td>
                                                <Table.Td miw="16rem"><Text size="xs" maw={300} lineClamp={2} title={item.commonSolves?.join(', ')}>{item.commonSolves?.join(', ') || '-'}</Text></Table.Td>
                                                <Table.Td miw="24rem"><ReadableDetails details={item.details} maxRows={4} /></Table.Td>
                                                <Table.Td miw="9rem">
                                                    <Button size="sm" variant="light" leftSection={<Icon path={mdiInformation} size={0.7} />} onClick={() => handleViewDetails(item)}>Details</Button>
                                                </Table.Td>
                                            </Table.Tr>
                                        ))}
                                    </Table.Tbody>
                                </Table>
                            </ScrollArea>
                        ) : (
                            <Alert color="green" icon={<Icon path={mdiCheckCircle} size={1} />}>No collusion groups detected</Alert>
                        )}
                    </Tabs.Panel>
                </Tabs>
            </Paper>
        </>
    )
}

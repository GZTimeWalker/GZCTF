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
    ActionIcon,
    Select,
    Loader,
} from '@mantine/core'
import { FC, useState, useMemo } from 'react'
import { Icon } from '@mdi/react'
import { mdiAlertCircle, mdiCheckCircle, mdiGhost, mdiIpNetwork, mdiShuffleVariant, mdiArrowUp, mdiArrowDown, mdiUnfoldMoreHorizontal, mdiInformation, mdiMagnify, mdiShieldAlert, mdiCog, mdiCancel, mdiCheck, mdiPencil, mdiChevronDown, mdiAccountGroup } from '@mdi/js'
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
}

function ThSort({ children, reversed, sorted, onSort, w }: ThSortProps) {
    const IconPath = sorted ? (reversed ? mdiArrowUp : mdiArrowDown) : mdiUnfoldMoreHorizontal
    return (
        <Table.Th w={w}>
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

export const CheatInfo: FC<CheatInfoProps> = ({ report, mutate }) => {
    const { t } = useTranslation()
    const { locale } = useLanguage()
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
                                <Table striped highlightOnHover>
                                    <Table.Thead>
                                        <Table.Tr>
                                            <Table.Th>Challenge</Table.Th>
                                            <Table.Th>Time A</Table.Th>
                                            <Table.Th>Time B</Table.Th>
                                            <Table.Th>Diff</Table.Th>
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>
                                        {drilledSolves.details.map((solve: SequenceSuspectDetail, idx: number) => (
                                            <Table.Tr key={idx}>
                                                <Table.Td fw={500}>
                                                    <ScrollingText text={solve.challengeName || 'Unknown'} size="sm" maw={200} />
                                                </Table.Td>
                                                <Table.Td ff="monospace" fz="sm">
                                                    {solve.timeA ? dayjs(solve.timeA).locale(locale).format('MM-DD HH:mm:ss') : '-'}
                                                </Table.Td>
                                                <Table.Td ff="monospace" fz="sm">
                                                    {solve.timeB ? dayjs(solve.timeB).locale(locale).format('MM-DD HH:mm:ss') : '-'}
                                                </Table.Td>
                                                <Table.Td>
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
                                <Text size="sm">
                                    {selectedGroup.details}
                                </Text>
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
                            <Table striped>
                                <Table.Thead>
                                    <Table.Tr>
                                        <Table.Th>Type</Table.Th>
                                        <Table.Th>Score Delta</Table.Th>
                                        <Table.Th>Time</Table.Th>
                                        <Table.Th>Details</Table.Th>
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {selectedSuspicion.events?.map((evt, idx) => (
                                        <Table.Tr key={idx}>
                                            <Table.Td>
                                                <Badge color={evt.type === 'Corroboration' ? 'grape' : 'blue'}>{evt.type}</Badge>
                                            </Table.Td>
                                            <Table.Td>+{evt.scoreDelta}</Table.Td>
                                            <Table.Td fz="xs" ff="monospace">
                                                {evt.time ? dayjs(evt.time).locale(locale).format('MM-DD HH:mm:ss') : '-'}
                                            </Table.Td>
                                            <Table.Td fz="sm">{evt.details}</Table.Td>
                                        </Table.Tr>
                                    ))}
                                </Table.Tbody>
                            </Table>
                        </ScrollArea>
                    </Stack>
                )}
            </Modal>

            {/* Summary Cards */}
            {/* ... Only replacing imports and upper logic, wait, I need to be careful with multi-defined blocks if I paste the whole file again ... */}
            {/* I will use TARGETED replacement for imports and specific logic blocks */}
            {/* ... Actually, the user asked for this, I will just overwrite the file since I have the whole content in mind and it's cleaner than partial patches that fail often ... */}
            {/* But I need to be sure about the middle content. */}

            <SimpleGrid cols={{ base: 1, md: 4 }} spacing="md">
                <Card shadow="sm" padding="md" radius="md" withBorder>
                    <Group justify="space-between" mb="xs">
                        <Text fw={500} c="red">High Risk Teams</Text>
                        <ThemeIcon color="red" variant="light">
                            <Icon path={mdiShieldAlert} size={0.8} />
                        </ThemeIcon>
                    </Group>
                    <Title order={3} c="red">
                        {report?.suspicionList?.filter((x: any) => (x.score ?? 0) >= 70).length ?? 0}
                    </Title>
                    <Text size="sm" c="dimmed">
                        Score {'>'}= 70
                    </Text>
                </Card>
                <Card shadow="sm" padding="md" radius="md" withBorder>
                    <Group justify="space-between" mb="xs">
                        <Text fw={500}>IP Anomalies</Text>
                        <ThemeIcon color="red" variant="light">
                            <Icon path={mdiIpNetwork} size={0.8} />
                        </ThemeIcon>
                    </Group>
                    <Title order={3}>{report?.ipAnalysis?.length ?? 0}</Title>
                    <Text size="sm" c="dimmed">
                        Suspicious IP activities
                    </Text>
                </Card>

                <Card shadow="sm" padding="md" radius="md" withBorder>
                    <Group justify="space-between" mb="xs">
                        <Text fw={500}>Abnormal Solves</Text>
                        <ThemeIcon color="orange" variant="light">
                            <Icon path={mdiGhost} size={0.8} />
                        </ThemeIcon>
                    </Group>
                    <Title order={3}>{report?.abnormalSolves?.length ?? 0}</Title>
                    <Text size="sm" c="dimmed">
                        Solves without prerequisites
                    </Text>
                </Card>


                <Card shadow="sm" padding="md" radius="md" withBorder>
                    <Group justify="space-between" mb="xs">
                        <Text fw={500}>Collusion Groups</Text>
                        <ThemeIcon color="red" variant="light">
                            <Icon path={mdiAccountGroup} size={0.8} />
                        </ThemeIcon>
                    </Group>
                    <Title order={3}>{report?.collusionGroups?.length ?? 0}</Title>
                    <Text size="sm" c="dimmed">
                        High confidence rings
                    </Text>
                </Card>
            </SimpleGrid>

            {/* Suspicion Analysis */}
            <Paper shadow="md" p="md">
                <Group justify="space-between" mb="md">
                    <Title order={4}>Suspicion Rankings</Title>
                </Group>
                {
                    report?.suspicionList && report.suspicionList.length > 0 ? (
                        <ScrollArea offsetScrollbars h="300px">
                            <Table className={tableClasses.table}>
                                <Table.Thead>
                                    <Table.Tr>
                                        <ThSort
                                            sorted={suspSort.key === 'teamName'}
                                            reversed={suspSort.direction === 'desc'}
                                            onSort={() => handleSort(setSuspSort, suspSort, 'teamName')}
                                            w="12rem"
                                        >
                                            Team
                                        </ThSort>
                                        <ThSort
                                            sorted={suspSort.key === 'score'}
                                            reversed={suspSort.direction === 'desc'}
                                            onSort={() => handleSort(setSuspSort, suspSort, 'score')}
                                            w="8rem"
                                        >
                                            Score
                                        </ThSort>
                                        <Table.Th>Status</Table.Th>
                                        <Table.Th>Actions</Table.Th>
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {sortedSuspicionList.map((item: any, index: number) => {
                                        const score = item.score ?? 0;
                                        const statusMap = useParticipationStatusMap()
                                        const currentStatus = item.status ?? ParticipationStatus.Pending
                                        const statusMeta = statusMap.get(currentStatus)

                                        const handleStatusChange = async (status: ParticipationStatus) => {
                                            try {
                                                await api.admin.adminParticipation(item.participationId!, { status })
                                                showNotification({
                                                    title: t('common.notify.success'),
                                                    message: t('common.notify.updated'),
                                                    color: 'green',
                                                })
                                                mutate?.()
                                            } catch (e: any) {
                                                showErrorMsg(e, t)
                                            }
                                        }

                                        return (
                                            <Table.Tr key={index}>
                                                <Table.Td fw={700}>
                                                    <ScrollingText text={item.teamName || 'Unknown'} size="sm" fw="bold" maw={200} />
                                                </Table.Td>
                                                <Table.Td>
                                                    <Badge color={score >= 70 ? 'red' : 'yellow'} size="lg" variant="light">{score}</Badge>
                                                </Table.Td>
                                                <Table.Td>
                                                    <Menu shadow="md" width={200}>
                                                        <Menu.Target>
                                                            <UnstyledButton style={{ cursor: 'pointer' }}>
                                                                <Badge
                                                                    color={statusMeta?.color || 'gray'}
                                                                    rightSection={<Icon path={mdiChevronDown} size={0.6} />}
                                                                >
                                                                    {statusMeta?.title || 'Unknown'}
                                                                </Badge>
                                                            </UnstyledButton>
                                                        </Menu.Target>

                                                        <Menu.Dropdown>
                                                            <Menu.Label>{t('common.label.status')}</Menu.Label>
                                                            {Array.from(statusMap.entries())
                                                                .filter(([status]) => status === ParticipationStatus.Accepted || status === ParticipationStatus.Suspended)
                                                                .map(([status, meta]) => (
                                                                    <Menu.Item
                                                                        key={status}
                                                                        leftSection={<Icon path={meta.iconPath} size={0.8} color={meta.color === 'alert' ? 'red' : meta.color} />}
                                                                        onClick={() => handleStatusChange(status)}
                                                                        disabled={currentStatus === status}
                                                                    >
                                                                        {meta.title}
                                                                    </Menu.Item>
                                                                ))}
                                                        </Menu.Dropdown>
                                                    </Menu>
                                                </Table.Td>
                                                <Table.Td>
                                                    <Button size="xs" variant="default" onClick={() => handleViewSuspicion(item)}>
                                                        View Details
                                                    </Button>
                                                </Table.Td>
                                            </Table.Tr>
                                        )
                                    })}
                                </Table.Tbody>
                            </Table>
                        </ScrollArea>
                    ) : (
                        <Alert color="green">No suspicion scores recorded</Alert>
                    )
                }
            </Paper >
            <Paper shadow="md" p="md">
                <Group justify="space-between" mb="md">
                    <Title order={4}>IP Analysis</Title>
                    <TextInput
                        placeholder="Search team, IP, or details..."
                        leftSection={<Icon path={mdiMagnify} size={0.8} />}
                        value={ipSearch}
                        onChange={(e) => setIpSearch(e.currentTarget.value)}
                        size="xs"
                        w={250}
                    />
                </Group>
                {report?.ipAnalysis && report.ipAnalysis.length > 0 ? (
                    <ScrollArea offsetScrollbars h="calc(33vh - 100px)">
                        <Table className={tableClasses.table}>
                            <Table.Thead>
                                <Table.Tr>
                                    <ThSort
                                        sorted={ipSort.key === 'teamName'}
                                        reversed={ipSort.direction === 'desc'}
                                        onSort={() => handleSort(setIpSort, ipSort, 'teamName')}
                                        w="10rem"
                                    >
                                        {t('common.label.team', 'Team')}
                                    </ThSort>
                                    <ThSort
                                        sorted={ipSort.key === 'type'}
                                        reversed={ipSort.direction === 'desc'}
                                        onSort={() => handleSort(setIpSort, ipSort, 'type')}
                                        w="12rem"
                                    >
                                        Type
                                    </ThSort>
                                    <ThSort
                                        sorted={ipSort.key === 'ip'}
                                        reversed={ipSort.direction === 'desc'}
                                        onSort={() => handleSort(setIpSort, ipSort, 'ip')}
                                        w="10rem"
                                    >
                                        IP
                                    </ThSort>
                                    <ThSort
                                        sorted={ipSort.key === 'time'}
                                        reversed={ipSort.direction === 'desc'}
                                        onSort={() => handleSort(setIpSort, ipSort, 'time')}
                                        w="10rem"
                                    >
                                        Time
                                    </ThSort>
                                    <Table.Th>Details</Table.Th>
                                </Table.Tr>
                            </Table.Thead>
                            <Table.Tbody>
                                {sortedIpAnalysis.map((item: any, index: number) => (
                                    <Table.Tr key={index}>
                                        <Table.Td>
                                            <ScrollingText text={item.teamName || 'Unknown'} size="sm" fw="bold" maw={150} />
                                        </Table.Td>
                                        <Table.Td w="12rem">
                                            <Badge
                                                color={
                                                    item.type === 'SharedIP' ? 'orange' :
                                                        item.type === 'SharedFingerprint' ? 'orange' :
                                                            item.type === 'CrossTeamIP' ? 'red' :
                                                                item.type === 'TokenAbuse' ? 'red' :
                                                                    'grape'
                                                }
                                                size="xs"
                                                fullWidth
                                            >
                                                {item.type === 'SharedIP' ? 'Shared IP' :
                                                    item.type === 'SharedFingerprint' ? 'Shared Fingerprint' :
                                                        item.type === 'CrossTeamIP' ? 'Cross-Team IP' :
                                                            item.type === 'TokenAbuse' ? 'Token Abuse' :
                                                                'Unknown IP'}
                                            </Badge>
                                        </Table.Td>
                                        <Table.Td ff="monospace">{item.ip}</Table.Td>
                                        <Table.Td ff="monospace" fz="xs">
                                            {item.time ? dayjs(item.time).locale(locale).format('MM-DD HH:mm:ss') : '-'}
                                        </Table.Td>
                                        <Table.Td>{item.details}</Table.Td>
                                    </Table.Tr>
                                ))}
                            </Table.Tbody>
                        </Table>
                    </ScrollArea>
                ) : (
                    <Alert color="green" icon={<Icon path={mdiCheckCircle} size={1} />}>
                        {t('game.content.no_cheat.title', 'No IP anomalies detected')}
                    </Alert>
                )}
            </Paper>

            {/* Abnormal Solves */}
            <Paper shadow="md" p="md">
                <Group justify="space-between" mb="md">
                    <Title order={4}>Abnormal Solves</Title>
                    <TextInput
                        placeholder="Search team, challenge, or type..."
                        leftSection={<Icon path={mdiMagnify} size={0.8} />}
                        value={solveSearch}
                        onChange={(e) => setSolveSearch(e.currentTarget.value)}
                        size="xs"
                        w={250}
                    />
                </Group>
                {report?.abnormalSolves && report.abnormalSolves.length > 0 ? (
                    <ScrollArea offsetScrollbars h="calc(33vh - 100px)">
                        <Table className={tableClasses.table}>
                            <Table.Thead>
                                <Table.Tr>
                                    <ThSort
                                        sorted={solveSort.key === 'teamName'}
                                        reversed={solveSort.direction === 'desc'}
                                        onSort={() => handleSort(setSolveSort, solveSort, 'teamName')}
                                        w="10rem"
                                    >
                                        {t('common.label.team', 'Team')}
                                    </ThSort>
                                    <ThSort
                                        sorted={solveSort.key === 'challengeName'}
                                        reversed={solveSort.direction === 'desc'}
                                        onSort={() => handleSort(setSolveSort, solveSort, 'challengeName')}
                                        w="12rem"
                                    >
                                        {t('common.label.challenge', 'Challenge')}
                                    </ThSort>
                                    <ThSort
                                        sorted={solveSort.key === 'type'}
                                        reversed={solveSort.direction === 'desc'}
                                        onSort={() => handleSort(setSolveSort, solveSort, 'type')}
                                        w="10rem"
                                    >
                                        Type
                                    </ThSort>
                                    <Table.Th>Details</Table.Th>
                                    <ThSort
                                        sorted={solveSort.key === 'solveTime'}
                                        reversed={solveSort.direction === 'desc'}
                                        onSort={() => handleSort(setSolveSort, solveSort, 'solveTime')}
                                        w="12rem"
                                    >
                                        {t('common.label.time', 'Time')}
                                    </ThSort>
                                </Table.Tr>
                            </Table.Thead>
                            <Table.Tbody>
                                {sortedAbnormalSolves.map((item: any, index: number) => (
                                    <Table.Tr key={index}>
                                        <Table.Td>
                                            <ScrollingText text={item.teamName || 'Unknown'} size="sm" fw="bold" maw={150} />
                                        </Table.Td>
                                        <Table.Td>
                                            <ScrollingText text={item.challengeName || 'Unknown'} size="sm" maw={180} />
                                        </Table.Td>
                                        <Table.Td>
                                            <Badge color={item.type === 'Hoarding' ? 'cyan' : 'orange'} size="sm">
                                                {item.type === 'NoDownload' ? 'No Download' : item.type === 'NoContainer' ? 'No Container' : item.type}
                                            </Badge>
                                        </Table.Td>
                                        <Table.Td fz="sm">
                                            {item.details}
                                        </Table.Td>
                                        <Table.Td ff="monospace">
                                            <Badge size="sm" color="indigo">
                                                {dayjs(item.solveTime).locale(locale).format('MM-DD HH:mm:ss')}
                                            </Badge>
                                        </Table.Td>
                                    </Table.Tr>
                                ))}
                            </Table.Tbody>
                        </Table>
                    </ScrollArea>
                ) : (
                    <Alert color="green" icon={<Icon path={mdiCheckCircle} size={1} />}>
                        {t('game.content.no_cheat.comment', 'No abnormal solves detected')}
                    </Alert>
                )}
            </Paper>



            {/* Collusion Groups */}
            <Paper shadow="md" p="md">
                <Group justify="space-between" mb="md">
                    <Title order={4}>Collusion Groups</Title>
                    <TextInput
                        placeholder="Search team name..."
                        leftSection={<Icon path={mdiMagnify} size={0.8} />}
                        value={collusionSearch}
                        onChange={(e) => setCollusionSearch(e.currentTarget.value)}
                        size="xs"
                        w={250}
                    />
                </Group>
                {report?.collusionGroups && report.collusionGroups.length > 0 ? (
                    <ScrollArea offsetScrollbars h="calc(33vh - 100px)">
                        <Table className={tableClasses.table}>
                            <Table.Thead>
                                <Table.Tr>
                                    <Table.Th w="20rem">Teams</Table.Th>
                                    <ThSort
                                        sorted={collusionSort.key === 'averageRsi'}
                                        reversed={collusionSort.direction === 'desc'}
                                        onSort={() => handleSort(setCollusionSort, collusionSort, 'averageRsi')}
                                        w="10rem"
                                    >
                                        Avg RSI
                                    </ThSort>
                                    <Table.Th>Common Solves</Table.Th>
                                    <Table.Th>Details</Table.Th>
                                    <Table.Th>Action</Table.Th>
                                </Table.Tr>
                            </Table.Thead>
                            <Table.Tbody>
                                {sortedCollusionGroups.map((item: any, index: number) => (
                                    <Table.Tr key={index}>
                                        <Table.Td>
                                            <Stack gap={2}>
                                                {item.teams?.map((team: CollusionTeamInfo, idx: number) => (
                                                    <ScrollingText key={idx} text={team.name} size="sm" fw="bold" maw={250} />
                                                ))}
                                            </Stack>
                                        </Table.Td>
                                        <Table.Td>
                                            <Badge color={(item.averageRsi ?? 0) > 0.8 ? 'red' : 'yellow'} size="sm">
                                                {((item.averageRsi ?? 0) * 100).toFixed(1)}%
                                            </Badge>
                                        </Table.Td>
                                        <Table.Td>
                                            <Text size="xs" maw={300} lineClamp={2} title={item.commonSolves?.join(', ')}>
                                                {item.commonSolves?.join(', ') || '-'}
                                            </Text>
                                        </Table.Td>
                                        <Table.Td>
                                            <Text size="xs" c="dimmed">
                                                {item.details}
                                            </Text>
                                        </Table.Td>
                                        <Table.Td>
                                            <Button size="xs" variant="light" leftSection={<Icon path={mdiInformation} size={0.7} />} onClick={() => handleViewDetails(item)}>
                                                Details
                                            </Button>
                                        </Table.Td>
                                    </Table.Tr>
                                ))}
                            </Table.Tbody>
                        </Table>
                    </ScrollArea>
                ) : (
                    <Alert color="green" icon={<Icon path={mdiCheckCircle} size={1} />}>
                        No collusion groups detected
                    </Alert>
                )}
            </Paper>
        </>
    )
}

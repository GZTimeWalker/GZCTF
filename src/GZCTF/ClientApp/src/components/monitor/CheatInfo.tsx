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
} from '@mantine/core'
import { FC, useState, useMemo } from 'react'
import { Icon } from '@mdi/react'
import { mdiAlertCircle, mdiCheckCircle, mdiGhost, mdiIpNetwork, mdiShuffleVariant, mdiArrowUp, mdiArrowDown, mdiUnfoldMoreHorizontal, mdiInformation } from '@mdi/js'
import { useTranslation } from 'react-i18next'
import dayjs from 'dayjs'
import { useLanguage } from '@Utils/I18n'
import { ScrollingText } from '@Components/ScrollingText'
import tableClasses from '@Styles/Table.module.css'
import type { CheatReport, SequenceSuspectResult, SequenceSuspectDetail } from '@Api'
import classes from './CheatInfo.module.css'
import { useDisclosure } from '@mantine/hooks'

interface CheatInfoProps {
    report: CheatReport | null
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

export const CheatInfo: FC<CheatInfoProps> = ({ report }) => {
    const { t } = useTranslation()
    const { locale } = useLanguage()

    // 1. IP Analysis Sort State
    const [ipSort, setIpSort] = useState<SortConfig<any>>({ key: null, direction: 'asc' })

    // 2. Abnormal Solves Sort State
    const [solveSort, setSolveSort] = useState<SortConfig<any>>({ key: null, direction: 'asc' })

    // 3. Sequence Similarity Sort State
    const [seqSort, setSeqSort] = useState<SortConfig<any>>({ key: 'similarity', direction: 'desc' })

    const [opened, { open, close }] = useDisclosure(false)
    const [selectedSuspect, setSelectedSuspect] = useState<SequenceSuspectResult | null>(null)

    const handleViewDetails = (item: SequenceSuspectResult) => {
        setSelectedSuspect(item)
        open()
    }

    const sortedIpAnalysis = useMemo(() => {
        if (!report?.ipAnalysis) return []
        return sortData(report.ipAnalysis, ipSort)
    }, [report?.ipAnalysis, ipSort])

    const sortedAbnormalSolves = useMemo(() => {
        if (!report?.abnormalSolves) return []
        return sortData(report.abnormalSolves, solveSort)
    }, [report?.abnormalSolves, solveSort])

    const sortedSequenceSuspects = useMemo(() => {
        if (!report?.sequenceSuspects) return []
        return sortData(report.sequenceSuspects, seqSort)
    }, [report?.sequenceSuspects, seqSort])

    const handleSort = (setSort: any, currentSort: any, key: string) => {
        const direction = currentSort.key === key && currentSort.direction === 'asc' ? 'desc' : 'asc'
        setSort({ key, direction })
    }

    return (
        <>
            <Modal opened={opened} onClose={close} title="Sequence Similarity Details" size="xl" centered>
                {selectedSuspect && (
                    <Stack>
                        <Group grow>
                            <Card withBorder padding="xs">
                                <Text size="xs" c="dimmed">Team A</Text>
                                <ScrollingText text={selectedSuspect.teamA || 'Unknown'} size="lg" fw={700} />
                            </Card>
                            <Center>
                                <Stack align="center" gap={0}>
                                    <Text size="xl" fw={900} c={((selectedSuspect.similarity ?? 0) > 0.9) ? 'red' : 'yellow'}>
                                        {((selectedSuspect.similarity ?? 0) * 100).toFixed(1)}%
                                    </Text>
                                    <Text size="xs" c="dimmed">Similarity</Text>
                                </Stack>
                            </Center>
                            <Card withBorder padding="xs" style={{ textAlign: 'right' }}>
                                <Text size="xs" c="dimmed" ta="right">Team B</Text>
                                <ScrollingText text={selectedSuspect.teamB || 'Unknown'} size="lg" fw={700} style={{ justifyContent: 'flex-end' }} />
                            </Card>
                        </Group>

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
                                    {selectedSuspect.detailedSolves?.map((solve: SequenceSuspectDetail, idx: number) => (
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
                    </Stack>
                )}
            </Modal>

            {/* Summary Cards */}
            {/* ... Only replacing imports and upper logic, wait, I need to be careful with multi-defined blocks if I paste the whole file again ... */}
            {/* I will use TARGETED replacement for imports and specific logic blocks */}
            {/* ... Actually, the user asked for this, I will just overwrite the file since I have the whole content in mind and it's cleaner than partial patches that fail often ... */}
            {/* But I need to be sure about the middle content. */}

            <SimpleGrid cols={{ base: 1, md: 3 }} spacing="md">
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
                        <Text fw={500}>Sequence Suspects</Text>
                        <ThemeIcon color="yellow" variant="light">
                            <Icon path={mdiShuffleVariant} size={0.8} />
                        </ThemeIcon>
                    </Group>
                    <Title order={3}>{report?.sequenceSuspects?.length ?? 0}</Title>
                    <Text size="sm" c="dimmed">
                        High similarity detected
                    </Text>
                </Card>
            </SimpleGrid>

            {/* IP Analysis */}
            <Paper shadow="md" p="md">
                <Title order={4} mb="md">IP Analysis</Title>
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
                                                        item.type === 'CrossTeamIP' ? 'red' :
                                                            'grape'
                                                }
                                                size="xs"
                                                fullWidth
                                            >
                                                {item.type === 'SharedIP' ? 'Shared IP' :
                                                    item.type === 'CrossTeamIP' ? 'Cross-Team IP' :
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
                <Title order={4} mb="md">Abnormal Solves</Title>
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

            {/* Sequence Similarity */}
            <Paper shadow="md" p="md">
                <Title order={4} mb="md">Sequence Similarity</Title>
                {report?.sequenceSuspects && report.sequenceSuspects.length > 0 ? (
                    <ScrollArea offsetScrollbars h="calc(33vh - 100px)">
                        <Table className={tableClasses.table}>
                            <Table.Thead>
                                <Table.Tr>
                                    <ThSort
                                        sorted={seqSort.key === 'teamA'}
                                        reversed={seqSort.direction === 'desc'}
                                        onSort={() => handleSort(setSeqSort, seqSort, 'teamA')}
                                        w="12rem"
                                    >
                                        Team A
                                    </ThSort>
                                    <ThSort
                                        sorted={seqSort.key === 'teamB'}
                                        reversed={seqSort.direction === 'desc'}
                                        onSort={() => handleSort(setSeqSort, seqSort, 'teamB')}
                                        w="12rem"
                                    >
                                        Team B
                                    </ThSort>
                                    <ThSort
                                        sorted={seqSort.key === 'similarity'}
                                        reversed={seqSort.direction === 'desc'}
                                        onSort={() => handleSort(setSeqSort, seqSort, 'similarity')}
                                        w="8rem"
                                    >
                                        Similarity
                                    </ThSort>
                                    <ThSort
                                        sorted={seqSort.key === 'commonSolves'}
                                        reversed={seqSort.direction === 'desc'}
                                        onSort={() => handleSort(setSeqSort, seqSort, 'commonSolves')}
                                        w="8rem"
                                    >
                                        Common Solves
                                    </ThSort>
                                    <Table.Th>Evidence</Table.Th>
                                    <Table.Th>Action</Table.Th>
                                </Table.Tr>
                            </Table.Thead>
                            <Table.Tbody>
                                {sortedSequenceSuspects.map((item: any, index: number) => (
                                    <Table.Tr key={index}>
                                        <Table.Td>
                                            <ScrollingText text={item.teamA || 'Unknown'} size="sm" fw="bold" maw={150} />
                                        </Table.Td>
                                        <Table.Td>
                                            <ScrollingText text={item.teamB || 'Unknown'} size="sm" fw="bold" maw={150} />
                                        </Table.Td>
                                        <Table.Td>
                                            <Badge color={(item.similarity ?? 0) > 0.9 ? 'red' : 'yellow'} size="sm">
                                                {((item.similarity ?? 0) * 100).toFixed(1)}%
                                            </Badge>
                                        </Table.Td>
                                        <Table.Td>{item.commonSolves ?? 0}</Table.Td>
                                        <Table.Td>
                                            <ScrollingText text={item.details || '-'} size="xs" maw={250} />
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
                        No suspicious sequence similarities detected
                    </Alert>
                )}
            </Paper>
        </>
    )
}

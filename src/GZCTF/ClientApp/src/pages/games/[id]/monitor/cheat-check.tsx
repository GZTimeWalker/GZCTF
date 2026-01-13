import {
    Badge,
    Group,
    Paper,
    ScrollArea,
    Stack,
    Table,
    Text,
    Title,
    Alert,
    SimpleGrid,
    Card,
    ThemeIcon,
} from '@mantine/core'
import { FC } from 'react'
import { useParams } from 'react-router'
import { WithGameMonitor } from '@Components/WithGameMonitor'
import api from '@Api'
import { Icon } from '@mdi/react'
import { mdiAlertCircle, mdiCheckCircle, mdiGhost, mdiIpNetwork, mdiShuffleVariant } from '@mdi/js'
import { useTranslation } from 'react-i18next'
import dayjs from 'dayjs'
import { useLanguage } from '@Utils/I18n'
import { ScrollingText } from '@Components/ScrollingText'
import tableClasses from '@Styles/Table.module.css'

const CheatDetection: FC = () => {
    const { id } = useParams()
    const numId = parseInt(id ?? '-1')
    const { t } = useTranslation()
    const { locale } = useLanguage()

    const { data: report, error, isLoading } = api.cheatReport.useCheatReportGet(numId)

    if (error) {
        return (
            <WithGameMonitor>
                <Alert color="red" title="Error" icon={<Icon path={mdiAlertCircle} size={1} />}>
                    Failed to load cheat report.
                </Alert>
            </WithGameMonitor>
        )
    }

    return (
        <WithGameMonitor isLoading={isLoading}>
            <Stack gap="md" w="100%">
                {/* Summary Cards */}
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
                                        <Table.Th w="10rem">{t('common.label.team', 'Team')}</Table.Th>
                                        <Table.Th w="12rem">Type</Table.Th>
                                        <Table.Th w="10rem">IP</Table.Th>
                                        <Table.Th>Details</Table.Th>
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {report.ipAnalysis.map((item: any, index: number) => (
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
                                        <Table.Th w="10rem">{t('common.label.team', 'Team')}</Table.Th>
                                        <Table.Th w="12rem">{t('common.label.challenge', 'Challenge')}</Table.Th>
                                        <Table.Th w="10rem">Type</Table.Th>
                                        <Table.Th w="12rem">{t('common.label.time', 'Time')}</Table.Th>
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {report.abnormalSolves.map((item: any, index: number) => (
                                        <Table.Tr key={index}>
                                            <Table.Td>
                                                <ScrollingText text={item.teamName || 'Unknown'} size="sm" fw="bold" maw={150} />
                                            </Table.Td>
                                            <Table.Td>
                                                <ScrollingText text={item.challengeName || 'Unknown'} size="sm" maw={180} />
                                            </Table.Td>
                                            <Table.Td>
                                                <Badge color="orange" size="sm">
                                                    {item.type === 'NoDownload' ? 'No Download' : 'No Container'}
                                                </Badge>
                                            </Table.Td>
                                            <Table.Td ff="monospace">
                                                <Badge size="sm" color="indigo">
                                                    {dayjs(item.solveTime).locale(locale).format('SL HH:mm:ss')}
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
                                        <Table.Th w="12rem">Team A</Table.Th>
                                        <Table.Th w="12rem">Team B</Table.Th>
                                        <Table.Th w="8rem">Similarity</Table.Th>
                                        <Table.Th w="8rem">Common Solves</Table.Th>
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {report.sequenceSuspects.map((item: any, index: number) => (
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
            </Stack>
        </WithGameMonitor>
    )
}

export default CheatDetection

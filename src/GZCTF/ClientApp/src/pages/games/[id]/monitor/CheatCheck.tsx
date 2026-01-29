import { useLanguage } from '@Utils/I18n'
import { CheatGraph } from '@Components/monitor/CheatGraph'
import { CheatInfo } from '@Components/monitor/CheatInfo'
import { CheatSubmissionLog } from '@Components/monitor/CheatSubmissionLog'
import { Loader, Stack, Title, Alert, Paper, Tabs } from '@mantine/core'
import { FC, useState } from 'react'
import { useParams, useSearchParams } from 'react-router'
import { WithGameMonitor } from '@Components/WithGameMonitor'
import api from '@Api'
import { useTranslation } from 'react-i18next'
import { Icon } from '@mdi/react'
import { mdiChartBox, mdiFlagVariant } from '@mdi/js'

const CheatCheck: FC = () => {
    const { id } = useParams()
    const numId = parseInt(id!)
    const { t } = useTranslation()
    const { locale } = useLanguage()

    // Tab state
    const [searchParams, setSearchParams] = useSearchParams()
    const tabFromUrl = searchParams.get('tab')
    const [activeTab, setActiveTab] = useState<string | null>(tabFromUrl || 'analysis')

    // Handle tab change and update URL
    const handleTabChange = (value: string | null) => {
        setActiveTab(value)
        setSearchParams({ tab: value || 'analysis' })
    }

    // Api call (for Analysis view)
    const { data: report, isLoading, error, mutate } = api.cheatReport.useCheatReportGet(numId, {
        revalidateOnFocus: false,
        revalidateOnReconnect: false,
        refreshInterval: 0
    })

    if (isLoading) return <WithGameMonitor><Loader /></WithGameMonitor>
    if (error) return <WithGameMonitor><Alert color="red">{error.message}</Alert></WithGameMonitor>

    return (
        <WithGameMonitor>
            <Stack gap="md" w="100%">
                <Title order={3}>{t('game.title.cheat_check', 'Cheat Analysis')}</Title>

                <Tabs value={activeTab} onChange={handleTabChange} variant="outline">
                    <Tabs.List>
                        <Tabs.Tab value="analysis" leftSection={<Icon path={mdiChartBox} size={0.8} />}>
                            Anomaly Analysis
                        </Tabs.Tab>
                        <Tabs.Tab value="submissions" leftSection={<Icon path={mdiFlagVariant} size={0.8} />}>
                            Submissions & Flags
                        </Tabs.Tab>
                    </Tabs.List>

                    <Tabs.Panel value="analysis" pt="md">
                        <Stack gap="md">
                            {/* Graph Visualization */}
                            <Paper shadow="md" p="md">
                                <Title order={4} mb="md">Relationship Graph</Title>
                                {report && (report.ipAnalysis?.length || report.sequenceSuspects?.length) ? (
                                    <CheatGraph report={report} />
                                ) : (
                                    <Alert color="gray">No relationship data to visualize</Alert>
                                )}
                            </Paper>

                            <CheatInfo report={report || null} mutate={mutate} />
                        </Stack>
                    </Tabs.Panel>

                    <Tabs.Panel value="submissions" pt="md">
                        <CheatSubmissionLog gameId={numId} />
                    </Tabs.Panel>
                </Tabs>
            </Stack>
        </WithGameMonitor>
    )
}

export default CheatCheck

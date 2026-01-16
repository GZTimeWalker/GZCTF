import { useLanguage } from '@Utils/I18n'
import { CheatGraph } from '@Components/monitor/CheatGraph'
import { CheatInfo } from '@Components/monitor/CheatInfo'
import { Loader, Stack, Title, Alert, Paper } from '@mantine/core'
import { FC } from 'react'
import { useParams } from 'react-router'
import { WithGameMonitor } from '@Components/WithGameMonitor'
import api from '@Api'
import { useTranslation } from 'react-i18next'

const CheatCheck: FC = () => {
    const { id } = useParams()
    const numId = parseInt(id!)
    const { t } = useTranslation()
    const { locale } = useLanguage()

    // Api call
    const { data: report, isLoading, error } = api.cheatReport.useCheatReportGet(numId, {
        revalidateOnFocus: false,
        revalidateOnReconnect: false,
        refreshInterval: 0
    })

    if (isLoading) return <WithGameMonitor><Loader /></WithGameMonitor>
    if (error) return <WithGameMonitor><Alert color="red">{error.message}</Alert></WithGameMonitor>

    return (
        <WithGameMonitor>
            <Stack gap="md" w="100%">
                <Title order={3}>{t('game.title.cheat_check', 'Cheat Check')}</Title>

                {/* Graph Visualization */}
                <Paper shadow="md" p="md">
                    <Title order={4} mb="md">Relationship Graph</Title>
                    {report && (report.ipAnalysis?.length || report.sequenceSuspects?.length) ? (
                        <CheatGraph report={report} />
                    ) : (
                        <Alert color="gray">No relationship data to visualize</Alert>
                    )}
                </Paper>

                <CheatInfo report={report || null} />
            </Stack>
        </WithGameMonitor>
    )
}

export default CheatCheck

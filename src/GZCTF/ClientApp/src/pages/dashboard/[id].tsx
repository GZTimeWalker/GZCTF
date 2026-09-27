import { Alert, Center, Loader, Select, Stack, Text, TextInput, Title } from '@mantine/core'
import { useParams, useSearchParams } from 'react-router'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { WithNavBar } from '@Components/WithNavbar'
import { SolveLeaderboard } from '@Components/dashboard/SolveLeaderboard'
import { SolveTrendChart } from '@Components/dashboard/SolveTrendChart'
import { useDashboard } from '@Hooks/useDashboard'

const DashboardPage = () => {
  const { id } = useParams()
  const [params] = useSearchParams()
  const token = params.get('token') ?? undefined
  const { t } = useTranslation('learning')
  const [cohortId, setCohortId] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const { data, error, connected } = useDashboard(id, token, cohortId ?? undefined, search || undefined)
  const highlighted = search
    ? data?.members.find(member => member.userName.toLowerCase().includes(search.toLowerCase()))?.userName
    : undefined
  return <WithNavBar minWidth={0}><Stack><Title order={1}>{t('dashboardTitle')}</Title>{error ? <Alert color="red">{t('dashboardInvalid')}</Alert> : !data ? <Center><Loader /></Center> : <><Select clearable label={t('dashboardCohort')} value={cohortId} onChange={setCohortId} data={data.cohorts.map((cohort) => ({ value: cohort.id, label: cohort.name }))} /><TextInput label={t('dashboardSearch')} value={search} onChange={(event) => setSearch(event.currentTarget.value)} /><Text size="sm" c={connected ? 'teal' : 'dimmed'}>{connected ? t('dashboardLive') : t('dashboardConnecting')}</Text><SolveTrendChart members={data.members} highlighted={highlighted} /><SolveLeaderboard snapshot={data} /></>}</Stack></WithNavBar>
}

export default DashboardPage

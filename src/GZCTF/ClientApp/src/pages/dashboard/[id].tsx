import { Alert, Center, Loader, Select, Stack, Text, TextInput, Title } from '@mantine/core'
import { useParams, useSearchParams } from 'react-router'
import { useState } from 'react'
import { WithNavBar } from '@Components/WithNavbar'
import { SolveLeaderboard } from '@Components/dashboard/SolveLeaderboard'
import { SolveTrendChart } from '@Components/dashboard/SolveTrendChart'
import { useDashboard } from '@Hooks/useDashboard'

const DashboardPage = () => {
  const { id } = useParams()
  const [params] = useSearchParams()
  const token = params.get('token') ?? undefined
  const [cohortId, setCohortId] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const { data, error, connected } = useDashboard(id, token, cohortId ?? undefined, search)
  return <WithNavBar minWidth={0}><Stack><Title order={1}>Learning activity</Title>{error ? <Alert color="red">Dashboard access expired or invalid.</Alert> : !data ? <Center><Loader /></Center> : <><Select clearable label="Cohort" value={cohortId} onChange={setCohortId} data={data.cohorts.map((cohort) => ({ value: cohort.id, label: cohort.name }))} /><TextInput label="Search username" value={search} onChange={(event) => setSearch(event.currentTarget.value)} /><Text size="sm" c={connected ? 'teal' : 'dimmed'}>{connected ? 'Live' : 'Connecting…'}</Text><SolveTrendChart members={data.members} highlighted={search || undefined} /><SolveLeaderboard snapshot={data} /></>}</Stack></WithNavBar>
}

export default DashboardPage


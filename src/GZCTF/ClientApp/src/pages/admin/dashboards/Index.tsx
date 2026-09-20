import { Button, Card, Group, NumberInput, Stack, Text, TextInput, Title } from '@mantine/core'
import useSWR from 'swr'
import { useState } from 'react'
import { fetcher } from '@Api'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { adminRequest } from '@Hooks/useAdminLearning'
import { Role } from '@Api'

type Dashboard = { id: string; name: string; topCount: number; isEnabled: boolean; activeTokenCount: number }

const AdminDashboards = () => {
  const { data: dashboards, mutate } = useSWR<Dashboard[]>('/api/admin/dashboards', fetcher)
  const [name, setName] = useState('')
  const [topCount, setTopCount] = useState<number | string>(10)
  const [rawToken, setRawToken] = useState<string>()
  const create = async () => { if (!name.trim()) return; await adminRequest('/api/admin/dashboards', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ name, topCount: Number(topCount) }) }); setName(''); await mutate() }
  const createToken = async (id: string) => { const result = await adminRequest<{ rawToken: string }>(`/api/admin/dashboards/${id}/tokens`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({}) }); setRawToken(`${window.location.origin}/dashboard/${id}?token=${result.rawToken}`) }
  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack><Title order={1}>Dashboards</Title><Card withBorder><Group><TextInput value={name} onChange={(event) => setName(event.currentTarget.value)} placeholder="Learning activity" /><NumberInput value={topCount} onChange={setTopCount} min={10} max={20} step={10} /><Button onClick={create}>Create dashboard</Button></Group></Card>{rawToken && <Text component="code">{rawToken}</Text>}{dashboards?.map((dashboard) => <Card key={dashboard.id} withBorder><Group justify="space-between"><Stack gap={0}><Text fw={600}>{dashboard.name}</Text><Text size="sm" c="dimmed">Top {dashboard.topCount} · {dashboard.activeTokenCount} active links</Text></Stack><Button onClick={() => createToken(dashboard.id)}>Create read-only link</Button></Group></Card>)}</Stack></WithNavBar></WithRole>
}

export default AdminDashboards


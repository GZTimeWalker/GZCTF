import { Button, Card, Group, Stack, Table, TextInput, Title } from '@mantine/core'
import useSWR from 'swr'
import { useState } from 'react'
import { fetcher } from '@Api'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { adminRequest } from '@Hooks/useAdminLearning'
import { Role } from '@Api'

type Cohort = { id: string; name: string; isActive: boolean; memberCount: number }

const AdminCohorts = () => {
  const { data: cohorts, mutate } = useSWR<Cohort[]>('/api/admin/cohorts', fetcher)
  const [name, setName] = useState('')
  const create = async () => { if (!name.trim()) return; await adminRequest('/api/admin/cohorts', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ name }) }); setName(''); await mutate() }
  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack><Title order={1}>Cohorts</Title><Card withBorder><Group><TextInput value={name} onChange={(event) => setName(event.currentTarget.value)} placeholder="24级" /><Button onClick={create}>Create cohort</Button></Group></Card><Table><Table.Thead><Table.Tr><Table.Th>Name</Table.Th><Table.Th>Status</Table.Th><Table.Th>Members</Table.Th></Table.Tr></Table.Thead><Table.Tbody>{cohorts?.map((cohort) => <Table.Tr key={cohort.id}><Table.Td>{cohort.name}</Table.Td><Table.Td>{cohort.isActive ? 'Active' : 'Inactive'}</Table.Td><Table.Td>{cohort.memberCount}</Table.Td></Table.Tr>)}</Table.Tbody></Table></Stack></WithNavBar></WithRole>
}

export default AdminCohorts


import { Button, Card, Group, SimpleGrid, Stack, Text, TextInput, Title } from '@mantine/core'
import { Link } from 'react-router'
import { useState } from 'react'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useAdminChallenges, adminRequest } from '@Hooks/useAdminLearning'
import { usePageTitle } from '@Hooks/usePageTitle'
import { Role } from '@Api'

const AdminChallenges = () => {
  const { data: challenges, mutate } = useAdminChallenges()
  const [title, setTitle] = useState('')
  usePageTitle('Challenge library')
  const create = async () => {
    if (!title.trim()) return
    await adminRequest('/api/admin/challenges', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ type: 'StaticAttachment', localizations: [{ locale: 'en', title, summary: '', body: '' }], flags: [] }) })
    setTitle('')
    await mutate()
  }
  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack><Title order={1}>Challenge library</Title><Group><TextInput value={title} onChange={(event) => setTitle(event.currentTarget.value)} placeholder="English title" /><Button onClick={create}>Create</Button></Group><SimpleGrid cols={{ base: 1, sm: 2 }}>{challenges?.map((challenge) => <Card key={challenge.id} withBorder component={Link} to={`/admin/library/challenges/${challenge.id}`}><Text fw={600}>{challenge.title}</Text><Text size="sm" c="dimmed">{challenge.type} · {challenge.publicationState}</Text></Card>)}</SimpleGrid></Stack></WithNavBar></WithRole>
}

export default AdminChallenges


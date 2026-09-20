import { Button, Card, SimpleGrid, Stack, Text, TextInput, Title } from '@mantine/core'
import { Link } from 'react-router'
import { useState } from 'react'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { adminRequest, useAdminLessons } from '@Hooks/useAdminLearning'
import { Role } from '@Api'

const AdminLessons = () => {
  const { data: lessons, mutate } = useAdminLessons()
  const [title, setTitle] = useState('')
  const create = async () => { if (!title.trim()) return; await adminRequest('/api/admin/lessons', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ localizations: [{ locale: 'en', title, body: '' }] }) }); setTitle(''); await mutate() }
  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack><Title order={1}>Lesson library</Title><Stack><TextInput value={title} onChange={(event) => setTitle(event.currentTarget.value)} placeholder="English title" /><Button onClick={create}>Create lesson</Button></Stack><SimpleGrid cols={{ base: 1, sm: 2 }}>{lessons?.map((lesson) => <Card key={lesson.id} withBorder component={Link} to={`/admin/library/lessons/${lesson.id}`}><Text fw={600}>{lesson.title}</Text></Card>)}</SimpleGrid></Stack></WithNavBar></WithRole>
}

export default AdminLessons


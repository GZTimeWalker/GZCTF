import { Button, Card, Group, SimpleGrid, Stack, Text, TextInput, Title } from '@mantine/core'
import { Link, useNavigate } from 'react-router'
import { useState } from 'react'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { adminRequest, useAdminPaths } from '@Hooks/useAdminLearning'
import { Role } from '@Api'

const AdminLearningPaths = () => {
  const { data: paths, mutate } = useAdminPaths()
  const [slug, setSlug] = useState('')
  const navigate = useNavigate()
  const create = async () => {
    if (!slug.trim()) return
    const result = await adminRequest<{ pathId: string }>('/api/admin/learning-paths', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ slug, localizations: [{ locale: 'en', title: slug, summary: '' }], modules: [] }) })
    setSlug('')
    await mutate()
    navigate(`/admin/learning-paths/${result.pathId}`)
  }
  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack><Title order={1}>Learning paths</Title><Group><TextInput value={slug} onChange={(event) => setSlug(event.currentTarget.value)} placeholder="route-slug" /><Button onClick={create}>Create route</Button></Group><SimpleGrid cols={{ base: 1, sm: 2 }}>{paths?.map((path) => <Card key={path.pathId} withBorder component={Link} to={`/admin/learning-paths/${path.pathId}`}><Text fw={600}>{path.title}</Text><Text size="sm" c="dimmed">{path.slug} · {path.isPublished ? 'Published' : 'Draft'}</Text></Card>)}</SimpleGrid></Stack></WithNavBar></WithRole>
}

export default AdminLearningPaths


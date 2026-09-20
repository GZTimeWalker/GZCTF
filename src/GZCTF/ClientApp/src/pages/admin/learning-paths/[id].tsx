import { Button, Stack, Text, Textarea, TextInput, Title } from '@mantine/core'
import { useParams } from 'react-router'
import useSWR from 'swr'
import { fetcher } from '@Api'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { adminRequest } from '@Hooks/useAdminLearning'
import { Role } from '@Api'
import { useState } from 'react'

const AdminLearningPathEdit = () => {
  const { id } = useParams()
  const { data, mutate } = useSWR<any>(id ? `/api/admin/learning-paths/${id}/draft?locale=en` : null, fetcher)
  const [slug, setSlug] = useState<string>()
  const [modules, setModules] = useState('[]')
  if (!id) return null
  const save = async () => {
    const parsed = JSON.parse(modules)
    await adminRequest(`/api/admin/learning-paths/${id}/draft`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ slug: slug ?? data?.slug ?? '', rowVersion: data?.rowVersion, localizations: data?.localizations ?? [{ locale: 'en', title: slug ?? data?.slug ?? '', summary: '' }], modules: parsed }) })
    await mutate()
  }
  const preview = async () => { await adminRequest(`/api/admin/learning-paths/${id}/publish`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ rowVersion: data?.rowVersion }) }); await mutate() }
  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack><Title order={1}>Route composer</Title>{!data ? <Text>Loading…</Text> : <><TextInput label="Slug" value={slug ?? data.slug} onChange={(event) => setSlug(event.currentTarget.value)} /><Textarea label="Modules JSON" minRows={18} value={modules} onChange={(event) => setModules(event.currentTarget.value)} /><Button onClick={save}>Save draft</Button><Button variant="light" onClick={preview}>Publish draft</Button><Text size="sm" c="dimmed">Publishing validates every module and item on the server.</Text></>}</Stack></WithNavBar></WithRole>
}

export default AdminLearningPathEdit


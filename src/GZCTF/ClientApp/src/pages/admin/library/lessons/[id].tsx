import { Button, Stack, Text, Textarea, TextInput, Title } from '@mantine/core'
import { useParams } from 'react-router'
import useSWR from 'swr'
import { fetcher } from '@Api'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { adminRequest } from '@Hooks/useAdminLearning'
import { Role } from '@Api'
import { useState } from 'react'

const AdminLessonEdit = () => {
  const { id } = useParams()
  const { data, mutate } = useSWR<any>(id ? `/api/admin/lessons/${id}?locale=en` : null, fetcher)
  const localization = data?.localizations?.find((item: any) => item.locale === 'en')
  const [title, setTitle] = useState<string>()
  const [body, setBody] = useState<string>()
  if (!id) return null
  const save = async () => { await adminRequest(`/api/admin/lessons/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ localizations: [{ locale: 'en', title: title ?? localization?.title ?? '', body: body ?? localization?.body ?? '' }] }) }); await mutate() }
  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack><Title order={1}>Edit lesson</Title>{!data ? <Text>Loading…</Text> : <><TextInput label="English title" value={title ?? localization?.title ?? ''} onChange={(event) => setTitle(event.currentTarget.value)} /><Textarea label="Markdown" minRows={16} value={body ?? localization?.body ?? ''} onChange={(event) => setBody(event.currentTarget.value)} /><Button onClick={save}>Save</Button></>}</Stack></WithNavBar></WithRole>
}

export default AdminLessonEdit


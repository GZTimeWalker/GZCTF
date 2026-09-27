import { Button, Card, SimpleGrid, Stack, Text, TextInput, Title } from '@mantine/core'
import { Link } from 'react-router'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useAdminLessons } from '@Hooks/useChallengeLibraryAdmin'
import api, { Role } from '@Api'
import { showErrorMsg } from '@Utils/Shared'

const AdminLessons = () => {
  const { data: lessons, mutate } = useAdminLessons()
  const { t } = useTranslation()
  const [title, setTitle] = useState('')
  const [creating, setCreating] = useState(false)
  const create = async () => {
    if (!title.trim()) return
    setCreating(true)
    try {
      await api.adminLessons.adminLessonsCreate({ locale: 'en', localizations: [{ locale: 'en', title, body: '' }] })
      setTitle('')
      await mutate()
    } catch (error) { showErrorMsg(error, t) } finally { setCreating(false) }
  }
  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack><Title order={1}>{t('learning:adminLessonLibrary')}</Title><Stack><TextInput value={title} onChange={(event) => setTitle(event.currentTarget.value)} placeholder={t('learning:adminEnglishTitle')} /><Button loading={creating} onClick={create}>{t('learning:adminCreateLesson')}</Button></Stack><SimpleGrid cols={{ base: 1, sm: 2 }}>{lessons?.map((lesson) => <Card key={lesson.id} withBorder component={Link} to={`/admin/library/lessons/${lesson.id}`}><Text fw={600}>{lesson.title}</Text></Card>)}</SimpleGrid></Stack></WithNavBar></WithRole>
}

export default AdminLessons

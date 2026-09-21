import { Button, Card, Group, SimpleGrid, Stack, Text, TextInput, Title } from '@mantine/core'
import { Link } from 'react-router'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useAdminChallenges } from '@Hooks/useChallengeLibraryAdmin'
import { usePageTitle } from '@Hooks/usePageTitle'
import api, { ChallengeType, Role } from '@Api'
import { showErrorMsg } from '@Utils/Shared'

const AdminChallenges = () => {
  const { data: challenges, mutate } = useAdminChallenges()
  const { t } = useTranslation()
  const [title, setTitle] = useState('')
  const [creating, setCreating] = useState(false)
  usePageTitle(t('learning:adminChallengeLibrary'))
  const create = async () => {
    if (!title.trim()) return
    setCreating(true)
    try {
      await api.adminChallenges.adminChallengesCreate({ type: ChallengeType.StaticAttachment, localizations: [{ locale: 'en', title, summary: '', body: '' }], flags: [] })
      setTitle('')
      await mutate()
    } catch (error) { showErrorMsg(error, t) } finally { setCreating(false) }
  }
  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack><Title order={1}>{t('learning:adminChallengeLibrary')}</Title><Group><TextInput value={title} onChange={(event) => setTitle(event.currentTarget.value)} placeholder={t('learning:adminEnglishTitle')} /><Button loading={creating} onClick={create}>{t('learning:adminCreate')}</Button></Group><SimpleGrid cols={{ base: 1, sm: 2 }}>{challenges?.map((challenge) => <Card key={challenge.id} withBorder component={Link} to={`/admin/library/challenges/${challenge.id}`}><Text fw={600}>{challenge.title}</Text><Text size="sm" c="dimmed">{challenge.type} · {challenge.publicationState}</Text></Card>)}</SimpleGrid></Stack></WithNavBar></WithRole>
}

export default AdminChallenges

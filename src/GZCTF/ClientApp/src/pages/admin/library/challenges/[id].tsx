import { Badge, Button, Group, Stack, Text, Textarea, TextInput, Title } from '@mantine/core'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import api, { Role } from '@Api'
import { ContentPublishModal } from '@Components/admin/library/ContentPublishModal'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { mergeChallengeLocalization } from '@Utils/LearningAdmin'
import { showErrorMsg } from '@Utils/Shared'

const AdminChallengeEdit = () => {
  const { id } = useParams()
  const { t } = useTranslation()
  const { t: tSkillTrees } = useTranslation('skillTrees')
  const { data, mutate } = api.adminChallenges.useAdminChallengesGetForEdit(
    id ?? '', { locale: 'en' }, undefined, Boolean(id)
  )
  const localization = data?.localizations?.find(item => item.locale?.toLowerCase() === 'en')
  const [title, setTitle] = useState<string>()
  const [body, setBody] = useState<string>()
  const [saving, setSaving] = useState(false)
  const [publishOpen, setPublishOpen] = useState(false)
  const [publishRowVersion, setPublishRowVersion] = useState<number>()
  if (!id) return null

  const save = async () => {
    setSaving(true)
    try {
      await api.adminChallenges.adminChallengesUpdate(id, {
        type: data?.challenge?.type,
        runtimeConfigurationJson: data?.runtimeConfigurationJson,
        localizations: mergeChallengeLocalization(data?.localizations ?? [], {
          locale: 'en',
          title: title ?? localization?.title ?? '',
          summary: localization?.summary ?? '',
          body: body ?? localization?.body ?? '',
        }),
        flags: data?.flags,
        hints: data?.hints,
        writeups: data?.writeups,
      })
      await mutate()
    } catch (error) {
      showErrorMsg(error, t)
    } finally {
      setSaving(false)
    }
  }

  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack>
    <Group justify="space-between">
      <Title order={1}>{t('learning:adminEditChallenge')}</Title>
      <Group>
        <Badge color={data?.challenge?.publicationState === 'Published' ? 'green' : 'gray'} variant="light">
          {tSkillTrees(`state.${(data?.challenge?.publicationState ?? 'Draft').toLowerCase()}`)}
        </Badge>
        <Button component={Link} to="/admin/skill-trees" variant="subtle">
          {tSkillTrees('list.title')}
        </Button>
      </Group>
    </Group>
    {!data ? <Text>{t('learning:loading')}</Text> : <>
      <TextInput label={t('learning:adminEnglishTitle')} value={title ?? localization?.title ?? ''} onChange={(event) => setTitle(event.currentTarget.value)} />
      <Textarea label={t('learning:adminStatement')} minRows={12} value={body ?? localization?.body ?? ''} onChange={(event) => setBody(event.currentTarget.value)} />
      <Group>
        <Button loading={saving} onClick={save}>{t('learning:adminSave')}</Button>
        <Button
          color="green"
          variant="light"
          loading={saving}
          onClick={async () => {
            await save()
            setPublishRowVersion(data.publication?.rowVersion)
            setPublishOpen(true)
          }}
        >
          {tSkillTrees('publish.challengeTitle')}
        </Button>
      </Group>
      <ContentPublishModal
        opened={publishOpen}
        kind="challenge"
        contentId={id}
        rowVersion={publishRowVersion ?? data.publication?.rowVersion ?? 0}
        initialCategoryIds={data.publication?.categoryIds ?? []}
        onClose={() => setPublishOpen(false)}
        onPublished={async () => { await mutate() }}
      />
    </>}
  </Stack></WithNavBar></WithRole>
}

export default AdminChallengeEdit

import { Alert, Badge, Button, Card, Group, Modal, Stack, Text, Title } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router'
import api, { ChallengeInstanceStatus, ChallengeType, Role, type ChallengeInstanceResponse } from '@Api'
import { ChallengeBasicsForm } from '@Components/admin/library/ChallengeBasicsForm'
import { ChallengeFlagEditor } from '@Components/admin/library/ChallengeFlagEditor'
import { ChallengeHelpEditor } from '@Components/admin/library/ChallengeHelpEditor'
import { ChallengeRuntimeForm } from '@Components/admin/library/ChallengeRuntimeForm'
import { ContentPublishModal } from '@Components/admin/library/ContentPublishModal'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { Markdown } from '@Components/MarkdownRenderer'
import { showErrorMsg } from '@Utils/Shared'
import { getApiErrorCode } from '@Utils/skillTreeAdmin'
import {
  challengeCommandFromDraft,
  challengeDraftFromResponse,
  isContainerType,
  type ChallengeDraft,
} from '@Utils/challengeEditor'

const AdminChallengeEdit = () => {
  const { id } = useParams()
  const navigate = useNavigate()
  const { t } = useTranslation('learning')
  const { t: tDefault } = useTranslation()
  const { t: tSkillTrees } = useTranslation('skillTrees')
  const { data, error, mutate } = api.adminChallenges.useAdminChallengesGetForEdit(
    id ?? '', { locale: 'en' }, undefined, Boolean(id)
  )
  const initializedId = useRef<string | undefined>(undefined)
  const saveInFlight = useRef(false)
  const uploadInFlight = useRef(false)
  const testInFlight = useRef(false)
  const [draft, setDraft] = useState<ChallengeDraft>()
  const [saving, setSaving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [testing, setTesting] = useState(false)
  const [publishOpen, setPublishOpen] = useState(false)
  const [previewOpen, setPreviewOpen] = useState(false)
  const [retireOpen, setRetireOpen] = useState(false)
  const [retiring, setRetiring] = useState(false)
  const [publishVersion, setPublishVersion] = useState<number>()
  const [testInstance, setTestInstance] = useState<ChallengeInstanceResponse>()
  const [formError, setFormError] = useState<string>()
  const [conflict, setConflict] = useState(false)
  const retired = data?.challenge?.publicationState === 'Retired' || data?.challenge?.publicationState === 'Merged'

  useEffect(() => {
    if (!id || !data || initializedId.current === id) return
    initializedId.current = id
    setDraft(challengeDraftFromResponse(data))
    setPublishVersion(data.publication?.rowVersion)
  }, [data, id])

  if (!id) return null

  const save = async (): Promise<number | undefined> => {
    if (!data || !draft || saveInFlight.current || uploadInFlight.current) return
    if (!draft.title.trim()) {
      setFormError(t('adminEnglishTitle'))
      return
    }
    saveInFlight.current = true
    setSaving(true)
    setFormError(undefined)
    try {
      const response = await api.adminChallenges.adminChallengesUpdate(
        id, challengeCommandFromDraft(draft, data, publishVersion)
      )
      setPublishVersion(response.data.publication?.rowVersion)
      await mutate(response.data, { revalidate: false })
      showNotification({ color: 'green', message: t('editorSaved') })
      return response.data.publication?.rowVersion
    } catch (saveError) {
      if (getApiErrorCode(saveError) === 'learning.challenge_changed') setConflict(true)
      else showErrorMsg(saveError, tDefault)
      return undefined
    } finally {
      saveInFlight.current = false
      setSaving(false)
    }
  }

  const publish = async () => {
    const version = await save()
    if (version !== undefined) {
      setPublishVersion(version)
      setPublishOpen(true)
    }
  }

  const reload = async () => {
    try {
      const fresh = await mutate()
      if (!fresh) return
      initializedId.current = id
      setDraft(challengeDraftFromResponse(fresh))
      setPublishVersion(fresh.publication?.rowVersion)
      setFormError(undefined)
      setConflict(false)
    } catch (reloadError) {
      showErrorMsg(reloadError, tDefault)
    }
  }

  const upload = async (file: File | null) => {
    if (!file || !draft || saveInFlight.current || uploadInFlight.current || testInFlight.current) return
    uploadInFlight.current = true
    setUploading(true)
    try {
      const response = await api.assets.assetsUpload({ files: [file] })
      const hash = response.data[0]?.hash
      if (!hash || hash.length !== 64) throw new Error('Attachment upload did not return a SHA-256 hash.')
      const entry = {
        FileName: file.name,
        Sha256: hash,
        StorageKey: `uploads/${hash.slice(0, 2)}/${hash.slice(2, 4)}/${hash}`,
        Flag: '',
      }
      setDraft((current) => current && ({
        ...current,
        attachments: current.type === ChallengeType.StaticAttachment ? [entry] : [...current.attachments, entry],
      }))
    } catch (uploadError) {
      showErrorMsg(uploadError, tDefault)
    } finally {
      uploadInFlight.current = false
      setUploading(false)
    }
  }

  const toggleTestContainer = async () => {
    if (!draft || !isContainerType(draft.type) || uploadInFlight.current || testInFlight.current) return
    testInFlight.current = true
    setTesting(true)
    try {
      if (testInstance?.status === ChallengeInstanceStatus.Running) {
        await api.challengeInstances.challengeInstancesStop(id)
        setTestInstance(undefined)
      } else {
        const version = await save()
        if (version === undefined) return
        const response = await api.challengeInstances.challengeInstancesStart(id)
        setTestInstance(response.data)
      }
    } catch (testError) {
      showErrorMsg(testError, tDefault)
    } finally {
      testInFlight.current = false
      setTesting(false)
    }
  }

  const retire = async () => {
    setRetiring(true)
    try {
      await api.adminChallenges.adminChallengesDelete(id)
      showNotification({ color: 'green', message: t('editorRetired') })
      navigate('/admin/skill-trees?tab=challenges')
    } catch (retireError) {
      showErrorMsg(retireError, tDefault)
    } finally {
      setRetiring(false)
    }
  }

  return (
    <WithRole requiredRole={Role.Admin}>
      <WithNavBar minWidth={0}>
        <Stack gap="lg" maw={1100} mx="auto">
          <Group justify="space-between" align="flex-start">
            <Stack gap={4}>
              <Title order={1}>{draft?.title ?? t('adminEditChallenge')}</Title>
              <Group gap="xs">
                <Badge variant="light" color={data?.challenge?.publicationState === 'Published' ? 'green' : 'gray'}>
                  {tSkillTrees(`state.${(data?.challenge?.publicationState ?? 'Draft').toLowerCase()}`)}
                </Badge>
                {draft && <Badge variant="outline">{draft.ctfCategory}</Badge>}
                {draft && <Badge variant="outline">{draft.type}</Badge>}
              </Group>
            </Stack>
            <Button component={Link} to="/admin/skill-trees?tab=challenges" variant="subtle">
              {tSkillTrees('workspace.tabs.challenges')}
            </Button>
          </Group>

          {error && <Alert color="red">{t('loadFailed')}</Alert>}
          {conflict && (
            <Alert color="red" title={t('editorChanged')}>
              <Button size="compact-sm" onClick={reload}>{t('editorReload')}</Button>
            </Alert>
          )}
          {!draft && !error ? <Text>{t('loading')}</Text> : draft && (
            <>
              {formError && <Alert color="red">{formError}</Alert>}
              <Card withBorder padding="lg">
                <ChallengeBasicsForm value={draft} onChange={setDraft} disabled={saving || testing || retired} />
              </Card>
              <Card withBorder padding="lg">
                <ChallengeRuntimeForm
                  value={draft} onChange={setDraft} onUpload={upload}
                  uploading={uploading} disabled={saving || testing || retired}
                />
                {isContainerType(draft.type) && (
                  <Stack mt="md" gap="xs" align="flex-start">
                    <Button variant="light" loading={testing} disabled={saving || uploading || retired} onClick={toggleTestContainer}>
                      {testInstance?.status === ChallengeInstanceStatus.Running
                        ? t('editorTestStop') : t('editorTestStart')}
                    </Button>
                    {testInstance?.status === ChallengeInstanceStatus.Running && testInstance.publicIp && (
                      <Text size="sm">
                        {t('editorTestAddress')}: {testInstance.publicIp}
                        {testInstance.publicPort ? `:${testInstance.publicPort}` : ''}
                      </Text>
                    )}
                  </Stack>
                )}
              </Card>
              <Card withBorder padding="lg">
                <ChallengeFlagEditor value={draft} onChange={setDraft} disabled={saving || testing || retired} />
              </Card>
              <Card withBorder padding="lg">
                <ChallengeHelpEditor value={draft} onChange={setDraft} disabled={saving || testing || retired} />
              </Card>
              <Group justify="flex-end">
                <Button color="red" variant="subtle" disabled={saving || uploading || testing || retired} onClick={() => setRetireOpen(true)}>
                  {t('editorRetireChallenge')}
                </Button>
                <Button variant="light" onClick={() => setPreviewOpen(true)}>{t('editorPreview')}</Button>
                <Button variant="light" loading={saving} disabled={uploading || testing || retired} onClick={save}>{t('editorSaveDraft')}</Button>
                <Button color="green" loading={saving} disabled={uploading || testing || retired} onClick={publish}>
                  {tSkillTrees('publish.challengeTitle')}
                </Button>
              </Group>
              <Modal opened={previewOpen} onClose={() => setPreviewOpen(false)} title={t('editorPreview')} size="lg">
                <Stack>
                  <Title order={3}>{draft.title}</Title>
                  <Text c="dimmed">{draft.summary}</Text>
                  <Markdown source={draft.body} />
                </Stack>
              </Modal>
              <Modal opened={retireOpen} onClose={() => setRetireOpen(false)} title={t('editorRetireChallenge')}>
                <Stack>
                  <Text>{t('editorRetireHint', { title: draft.title })}</Text>
                  <Group justify="flex-end">
                    <Button variant="default" onClick={() => setRetireOpen(false)}>{t('editorCancel')}</Button>
                    <Button color="red" loading={retiring} onClick={retire}>{t('editorRetire')}</Button>
                  </Group>
                </Stack>
              </Modal>
              <ContentPublishModal
                opened={publishOpen}
                kind="challenge"
                contentId={id}
                rowVersion={publishVersion ?? data?.publication?.rowVersion ?? 0}
                initialCategoryIds={data?.publication?.categoryIds ?? []}
                onClose={() => setPublishOpen(false)}
                onPublished={async () => { await mutate() }}
              />
            </>
          )}
        </Stack>
      </WithNavBar>
    </WithRole>
  )
}

export default AdminChallengeEdit

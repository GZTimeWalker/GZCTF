import { Badge, Button, Card, Group, Modal, Select, SimpleGrid, Stack, Switch, Text, TextInput } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import api, { ChallengeCategory, ChallengeType, type ChallengeSummaryResponse } from '@Api'
import { useAdminChallenges } from '@Hooks/useChallengeLibraryAdmin'
import { showErrorMsg } from '@Utils/Shared'
import { getApiErrorCode } from '@Utils/skillTreeAdmin'
import classes from '@Components/admin/workspace/AdminWorkspace.module.css'
import type { CreatePanelProps } from '@Components/admin/workspace/types'

export const ChallengesPanel = ({ createOpen, onClose, onOpen }: CreatePanelProps) => {
  // The learning namespace must be requested explicitly; `learning:`-prefixed
  // keys passed to a default-namespace t never trigger its load.
  const { t } = useTranslation('learning')
  const { t: tDefault } = useTranslation()
  const { t: tSkillTrees } = useTranslation('skillTrees')
  const { t: tChallenge } = useTranslation('challenge')
  const navigate = useNavigate()
  const { data: challenges, error, isLoading, mutate } = useAdminChallenges()
  const [title, setTitle] = useState('')
  const [category, setCategory] = useState<string | null>(null)
  const [type, setType] = useState<string | null>(null)
  const [filterCategory, setFilterCategory] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [toggleTarget, setToggleTarget] = useState<ChallengeSummaryResponse>()
  const [toggling, setToggling] = useState(false)
  const visibleChallenges = challenges?.filter((challenge) =>
    !filterCategory || challenge.ctfCategory === filterCategory)

  const toggleAvailability = async () => {
    if (!toggleTarget?.id) return
    setToggling(true)
    try {
      await api.adminChallenges.adminChallengesUpdate(toggleTarget.id, {
        rowVersion: toggleTarget.rowVersion,
        isEnabled: !toggleTarget.isEnabled,
      })
      await mutate()
      showNotification({ color: 'green', message: t('editorAvailabilitySaved') })
      setToggleTarget(undefined)
    } catch (error) {
      if (getApiErrorCode(error) === 'learning.challenge_changed') {
        setToggleTarget(undefined)
        await mutate()
        showNotification({ color: 'red', message: t('editorAvailabilityConflict') })
      } else {
        showErrorMsg(error, tDefault)
      }
    } finally {
      setToggling(false)
    }
  }

  const create = async () => {
    if (!title.trim() || !category || !type) return
    setCreating(true)
    try {
      const result = await api.adminChallenges.adminChallengesCreate({
        type: type as ChallengeType,
        ctfCategory: category as ChallengeCategory,
        localizations: [{ locale: 'en', title: title.trim(), summary: '', body: '' }],
        flags: [],
      })
      setTitle('')
      setCategory(null)
      setType(null)
      onClose()
      showNotification({ color: 'green', message: tSkillTrees('workspace.challengeCreated') })
      await mutate()
      if (result.data.challenge?.id) navigate(`/admin/library/challenges/${result.data.challenge.id}`)
    } catch (err) {
      showErrorMsg(err, tDefault)
    } finally {
      setCreating(false)
    }
  }

  return (
    <Stack gap="md">
      {challenges && challenges.length > 0 && (
        <Select
          label={t('editorFilterCategory')}
          placeholder={t('editorAllCategories')}
          data={Object.values(ChallengeCategory).map((value) => ({
            value,
            label: `${value} · ${tChallenge(`category.${value.toLowerCase()}`)}`,
          }))}
          value={filterCategory}
          onChange={setFilterCategory}
          clearable searchable
          maw={320}
        />
      )}
      {isLoading ? (
        <Text c="dimmed">{t('learning:loading')}</Text>
      ) : error ? (
        <div className={classes.emptyState}>
          <Text c="red">{t('learning:loadFailed')}</Text>
          <Button variant="light" onClick={() => mutate()}>
            {tSkillTrees('workspace.retry')}
          </Button>
        </div>
      ) : visibleChallenges && visibleChallenges.length > 0 ? (
        <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
          {visibleChallenges.map((challenge) => (
            <Card key={challenge.id} withBorder className={classes.panelCard}>
              <Group justify="space-between" wrap="nowrap">
                <Text fw={600} lineClamp={1} component={Link} to={`/admin/library/challenges/${challenge.id}`}>
                  {challenge.title}
                </Text>
                <Badge
                  color={challenge.publicationState === 'Published' ? 'green' : 'gray'}
                  variant="light"
                >
                  {tSkillTrees(`state.${(challenge.publicationState ?? 'Draft').toLowerCase()}`)}
                </Badge>
              </Group>
              <Group justify="space-between" align="flex-end" mt="xs" wrap="wrap">
                <Group gap="xs">
                  <Badge variant="outline">{challenge.ctfCategory ?? ChallengeCategory.Misc}</Badge>
                  <Text size="sm" c="dimmed">{challenge.type}</Text>
                </Group>
                <Switch
                  size="sm"
                  label={t('editorEnabled')}
                  checked={challenge.isEnabled ?? false}
                  disabled={challenge.publicationState === 'Retired' || challenge.publicationState === 'Merged'}
                  onChange={() => setToggleTarget(challenge)}
                />
              </Group>
            </Card>
          ))}
        </SimpleGrid>
      ) : filterCategory && challenges?.length ? (
        <Text c="dimmed">{t('editorNoFilteredChallenges')}</Text>
      ) : (
        <div className={classes.emptyState}>
          <Text c="dimmed">{tSkillTrees('workspace.emptyChallenges')}</Text>
          <Button variant="light" onClick={onOpen}>
            {tSkillTrees('workspace.createChallenge')}
          </Button>
        </div>
      )}

      <Modal opened={createOpen} onClose={onClose} title={tSkillTrees('workspace.createChallenge')}>
        <Stack>
          <TextInput
            label={t('adminTitle')}
            value={title}
            onChange={(event) => setTitle(event.currentTarget.value)}
          />
          <Select
            label={t('adminCtfCategory')}
            data={Object.values(ChallengeCategory).map((value) => ({
              value,
              label: `${value} · ${tChallenge(`category.${value.toLowerCase()}`)}`,
            }))}
            value={category}
            onChange={setCategory}
            searchable
            required
          />
          <Select
            label={t('adminRuntimeType')}
            data={[
              { value: ChallengeType.StaticAttachment, label: tChallenge('type.static_attachment.label') },
              { value: ChallengeType.StaticContainer, label: tChallenge('type.static_container.label') },
              { value: ChallengeType.DynamicAttachment, label: tChallenge('type.dynamic_attachment.label') },
              { value: ChallengeType.DynamicContainer, label: tChallenge('type.dynamic_container.label') },
            ]}
            value={type}
            onChange={setType}
            required
          />
          <Button loading={creating} disabled={!title.trim() || !category || !type} onClick={create} w="fit-content">
            {t('learning:adminCreate')}
          </Button>
        </Stack>
      </Modal>

      <Modal
        opened={Boolean(toggleTarget)}
        onClose={() => setToggleTarget(undefined)}
        title={toggleTarget?.isEnabled ? t('editorDisableChallenge') : t('editorEnableChallenge')}
      >
        <Stack>
          <Text size="sm">{t('editorToggleHint', { title: toggleTarget?.title })}</Text>
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setToggleTarget(undefined)}>{t('editorCancel')}</Button>
            <Button loading={toggling} onClick={toggleAvailability}>
              {toggleTarget?.isEnabled ? t('editorDisableChallenge') : t('editorEnableChallenge')}
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  )
}

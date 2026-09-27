import { Badge, Button, Card, Group, Modal, SimpleGrid, Stack, Text, TextInput } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import api, { ChallengeType } from '@Api'
import { useAdminChallenges } from '@Hooks/useChallengeLibraryAdmin'
import { showErrorMsg } from '@Utils/Shared'
import classes from '@Components/admin/workspace/AdminWorkspace.module.css'
import type { CreatePanelProps } from '@Components/admin/workspace/types'

export const ChallengesPanel = ({ createOpen, onClose, onOpen }: CreatePanelProps) => {
  // The learning namespace must be requested explicitly; `learning:`-prefixed
  // keys passed to a default-namespace t never trigger its load.
  const { t } = useTranslation('learning')
  const { t: tDefault } = useTranslation()
  const { t: tSkillTrees } = useTranslation('skillTrees')
  const { data: challenges, error, isLoading, mutate } = useAdminChallenges()
  const [title, setTitle] = useState('')
  const [creating, setCreating] = useState(false)

  const create = async () => {
    if (!title.trim()) return
    setCreating(true)
    try {
      await api.adminChallenges.adminChallengesCreate({
        type: ChallengeType.StaticAttachment,
        localizations: [{ locale: 'en', title: title.trim(), summary: '', body: '' }],
        flags: [],
      })
      setTitle('')
      onClose()
      showNotification({ color: 'green', message: tSkillTrees('workspace.challengeCreated') })
      await mutate()
    } catch (err) {
      showErrorMsg(err, tDefault)
    } finally {
      setCreating(false)
    }
  }

  return (
    <Stack gap="md">
      {isLoading ? (
        <Text c="dimmed">{t('learning:loading')}</Text>
      ) : error ? (
        <div className={classes.emptyState}>
          <Text c="red">{t('learning:loadFailed')}</Text>
          <Button variant="light" onClick={() => mutate()}>
            {tSkillTrees('workspace.retry')}
          </Button>
        </div>
      ) : challenges && challenges.length > 0 ? (
        <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
          {challenges.map((challenge) => (
            <Card
              key={challenge.id}
              withBorder
              className={classes.panelCard}
              component={Link}
              to={`/admin/library/challenges/${challenge.id}`}
            >
              <Group justify="space-between" wrap="nowrap">
                <Text fw={600} lineClamp={1}>
                  {challenge.title}
                </Text>
                <Badge
                  color={challenge.publicationState === 'Published' ? 'green' : 'gray'}
                  variant="light"
                >
                  {tSkillTrees(`state.${(challenge.publicationState ?? 'Draft').toLowerCase()}`)}
                </Badge>
              </Group>
              <Text size="sm" c="dimmed" mt="xs">
                {challenge.type}
              </Text>
            </Card>
          ))}
        </SimpleGrid>
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
            label={t('learning:adminEnglishTitle')}
            value={title}
            onChange={(event) => setTitle(event.currentTarget.value)}
          />
          <Button loading={creating} onClick={create} w="fit-content">
            {t('learning:adminCreate')}
          </Button>
        </Stack>
      </Modal>
    </Stack>
  )
}

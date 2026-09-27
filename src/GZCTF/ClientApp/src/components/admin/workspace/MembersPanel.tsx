import { Badge, Button, Card, Group, Modal, MultiSelect, SimpleGrid, Stack, Table, Text, TextInput, UnstyledButton } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import api, { type UserInfoModel } from '@Api'
import { showErrorMsg } from '@Utils/Shared'
import classes from '@Components/admin/workspace/AdminWorkspace.module.css'
import type { CreatePanelProps } from '@Components/admin/workspace/types'

export const MembersPanel = ({ createOpen, onClose, onOpen }: CreatePanelProps) => {
  // The learning namespace must be requested explicitly; `learning:`-prefixed
  // keys passed to a default-namespace t never trigger its load.
  const { t } = useTranslation('learning')
  const { t: tDefault } = useTranslation()
  const { t: tSkillTrees } = useTranslation('skillTrees')
  const { data: cohorts, error: cohortsError, isLoading: cohortsLoading, mutate: mutateCohorts } = api.adminCohorts.useAdminCohortsList()
  const [name, setName] = useState('')
  const [selectedCohortId, setSelectedCohortId] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [candidates, setCandidates] = useState<UserInfoModel[]>([])
  const [searched, setSearched] = useState(false)
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([])
  const [pending, setPending] = useState(false)
  const { data: members, mutate: mutateMembers } = api.adminCohorts.useAdminCohortsMembers(
    selectedCohortId ?? '',
    undefined,
    undefined,
    Boolean(selectedCohortId)
  )

  useEffect(() => {
    if (!selectedCohortId && cohorts?.[0]?.id) setSelectedCohortId(cohorts[0].id)
  }, [cohorts, selectedCohortId])

  const selectedCohort = cohorts?.find((cohort) => cohort.id === selectedCohortId)
  const selectCohort = (id: string | null) => {
    setSelectedCohortId(id)
    setSelectedUserIds([])
  }
  const candidateOptions = useMemo(
    () =>
      candidates
        .filter((user) => user.id)
        .map((user) => ({ value: user.id!, label: user.userName ?? user.id! })),
    [candidates]
  )

  const run = async (action: () => Promise<void>) => {
    setPending(true)
    try {
      await action()
    } catch (error) {
      showErrorMsg(error, tDefault)
    } finally {
      setPending(false)
    }
  }

  const create = () =>
    run(async () => {
      if (!name.trim()) return
      const result = await api.adminCohorts.adminCohortsCreate({ name: name.trim() })
      setName('')
      onClose()
      setSelectedCohortId(result.data.id ?? null)
      await mutateCohorts()
      showNotification({ color: 'green', message: tSkillTrees('workspace.cohortCreated') })
    })

  const findUsers = () =>
    run(async () => {
      const result = await api.admin.adminSearchUsers({ hint: search.trim() })
      setCandidates(result.data.data)
      setSearched(true)
    })

  const assign = () =>
    run(async () => {
      if (!selectedCohortId || selectedUserIds.length === 0) return
      await api.adminCohorts.adminCohortsAssign(selectedCohortId, { userIds: selectedUserIds })
      setSelectedUserIds([])
      await Promise.all([mutateMembers(), mutateCohorts()])
      showNotification({ color: 'green', message: tSkillTrees('workspace.membersAssigned') })
    })

  const clear = (userId: string) =>
    run(async () => {
      if (!selectedCohortId) return
      await api.adminCohorts.adminCohortsClear(selectedCohortId, userId)
      await Promise.all([mutateMembers(), mutateCohorts()])
      showNotification({ color: 'green', message: tSkillTrees('workspace.memberRemoved') })
    })

  const hasCohorts = Boolean(cohorts && cohorts.length > 0)

  return (
    <Stack gap="md">
      {cohortsLoading ? (
        <Text c="dimmed">{t('learning:loading')}</Text>
      ) : cohortsError ? (
        <div className={classes.emptyState}>
          <Text c="red">{t('learning:loadFailed')}</Text>
          <Button variant="light" onClick={() => void mutateCohorts()}>
            {tSkillTrees('workspace.retry')}
          </Button>
        </div>
      ) : !hasCohorts ? (
        <div className={classes.emptyState}>
          <Text c="dimmed">{tSkillTrees('workspace.emptyCohorts')}</Text>
          <Button variant="light" onClick={onOpen}>
            {tSkillTrees('workspace.createCohort')}
          </Button>
        </div>
      ) : (
        <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md">
          <Card withBorder className={classes.panelCard}>
            <Stack gap="sm">
              <Text fw={600}>{t('learning:adminCohorts')}</Text>
              <Stack gap={4}>
                {cohorts?.map((cohort) => (
                  <UnstyledButton
                    key={cohort.id}
                    className={cohort.id === selectedCohortId ? classes.cohortRowActive : classes.cohortRow}
                    onClick={() => selectCohort(cohort.id ?? null)}
                  >
                    <Group justify="space-between" wrap="nowrap">
                      <Group gap="xs" wrap="nowrap">
                        <Text fw={500}>{cohort.name}</Text>
                        {cohort.isActive === false && (
                          <Badge color="gray" variant="light">
                            {t('learning:adminInactive')}
                          </Badge>
                        )}
                      </Group>
                      <Badge variant="outline">
                        {t('learning:adminMembers')} · {cohort.memberCount}
                      </Badge>
                    </Group>
                  </UnstyledButton>
                ))}
              </Stack>
            </Stack>
          </Card>

          <Stack gap="md">
            <Card withBorder>
              <Stack gap="sm">
                <Text fw={600}>{selectedCohort?.name ?? t('learning:adminSelectedCohort')}</Text>
                <Group align="end" gap="sm">
                  <TextInput
                    label={t('learning:adminSearchUsers')}
                    value={search}
                    className={classes.growInput}
                    onChange={(event) => setSearch(event.currentTarget.value)}
                  />
                  <Button variant="light" loading={pending} onClick={findUsers}>
                    {t('learning:adminSearch')}
                  </Button>
                </Group>
                <MultiSelect
                  searchable
                  label={t('learning:adminAssignMembers')}
                  data={candidateOptions}
                  value={selectedUserIds}
                  onChange={setSelectedUserIds}
                  nothingFoundMessage={t('learning:adminNoUsers')}
                />
                {searched && candidates.length === 0 && (
                  <Text size="sm" c="dimmed">
                    {t('learning:adminNoUsers')}
                  </Text>
                )}
                <Button
                  loading={pending}
                  disabled={!selectedCohortId || selectedUserIds.length === 0}
                  onClick={assign}
                  w="fit-content"
                >
                  {t('learning:adminAssignSelected')}
                </Button>
              </Stack>
            </Card>

            <Card withBorder>
              <Stack gap="sm">
                <Text fw={600}>{t('learning:adminCurrentMembers')}</Text>
                {members && members.length > 0 ? (
                  <Table verticalSpacing="xs">
                    <Table.Tbody>
                      {members.map((member) => (
                        <Table.Tr key={member.id}>
                          <Table.Td>{member.userName}</Table.Td>
                          <Table.Td w={1}>
                            <Button
                              size="compact-xs"
                              color="red"
                              variant="light"
                              loading={pending}
                              onClick={() => member.id && clear(member.id)}
                            >
                              {t('learning:adminRemoveCohort')}
                            </Button>
                          </Table.Td>
                        </Table.Tr>
                      ))}
                    </Table.Tbody>
                  </Table>
                ) : (
                  <Text size="sm" c="dimmed">
                    {selectedCohortId ? tSkillTrees('workspace.emptyMembers') : tSkillTrees('workspace.emptyCohorts')}
                  </Text>
                )}
              </Stack>
            </Card>
          </Stack>
        </SimpleGrid>
      )}

      <Modal opened={createOpen} onClose={onClose} title={tSkillTrees('workspace.createCohort')}>
        <Stack>
          <TextInput
            label={t('learning:adminCohortName')}
            placeholder={t('learning:adminCohortPlaceholder')}
            value={name}
            onChange={(event) => setName(event.currentTarget.value)}
          />
          <Button loading={pending} onClick={create} w="fit-content">
            {t('learning:adminCreate')}
          </Button>
        </Stack>
      </Modal>
    </Stack>
  )
}

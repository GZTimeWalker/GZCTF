import { Button, Card, Group, MultiSelect, Select, Stack, Table, Text, TextInput, Title } from '@mantine/core'
import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import api, { Role, UserInfoModel } from '@Api'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { showErrorMsg } from '@Utils/Shared'

const AdminCohorts = () => {
  const { data: cohorts, mutate: mutateCohorts } = api.adminCohorts.useAdminCohortsList()
  const { t } = useTranslation()
  const [name, setName] = useState('')
  const [selectedCohortId, setSelectedCohortId] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [candidates, setCandidates] = useState<UserInfoModel[]>([])
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([])
  const [pending, setPending] = useState(false)
  const { data: members, mutate: mutateMembers } = api.adminCohorts.useAdminCohortsMembers(
    selectedCohortId ?? '', undefined, undefined, Boolean(selectedCohortId)
  )

  useEffect(() => {
    if (!selectedCohortId && cohorts?.[0]?.id) setSelectedCohortId(cohorts[0].id)
  }, [cohorts, selectedCohortId])

  const cohortOptions = useMemo(() => (cohorts ?? [])
    .filter(cohort => cohort.id)
    .map(cohort => ({ value: cohort.id!, label: cohort.name ?? '' })), [cohorts])
  const candidateOptions = useMemo(() => candidates
    .filter(user => user.id)
    .map(user => ({ value: user.id!, label: user.userName ?? user.id! })), [candidates])

  const run = async (action: () => Promise<void>) => {
    setPending(true)
    try { await action() } catch (error) { showErrorMsg(error, t) } finally { setPending(false) }
  }

  const create = () => run(async () => {
    if (!name.trim()) return
    const result = await api.adminCohorts.adminCohortsCreate({ name: name.trim() })
    setName('')
    setSelectedCohortId(result.data.id ?? null)
    await mutateCohorts()
  })

  const findUsers = () => run(async () => {
    const result = await api.admin.adminSearchUsers({ hint: search.trim() })
    setCandidates(result.data.data)
  })

  const assign = () => run(async () => {
    if (!selectedCohortId || selectedUserIds.length === 0) return
    await api.adminCohorts.adminCohortsAssign(selectedCohortId, { userIds: selectedUserIds })
    setSelectedUserIds([])
    await Promise.all([mutateMembers(), mutateCohorts()])
  })

  const clear = (userId: string) => run(async () => {
    if (!selectedCohortId) return
    await api.adminCohorts.adminCohortsClear(selectedCohortId, userId)
    await Promise.all([mutateMembers(), mutateCohorts()])
  })

  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack>
    <Title order={1}>{t('learning:adminCohorts')}</Title>
    <Card withBorder>
      <Group align="end">
        <TextInput value={name} onChange={(event) => setName(event.currentTarget.value)} placeholder={t('learning:adminCohortPlaceholder')} label={t('learning:adminCohortName')} />
        <Button loading={pending} onClick={create}>{t('learning:adminCreateCohort')}</Button>
      </Group>
    </Card>

    <Table withTableBorder striped>
      <Table.Thead><Table.Tr><Table.Th>{t('learning:adminName')}</Table.Th><Table.Th>{t('learning:adminStatus')}</Table.Th><Table.Th>{t('learning:adminMembers')}</Table.Th></Table.Tr></Table.Thead>
      <Table.Tbody>{cohorts?.map((cohort) => <Table.Tr key={cohort.id} onClick={() => setSelectedCohortId(cohort.id ?? null)} style={{ cursor: 'pointer' }}>
        <Table.Td>{cohort.name}</Table.Td><Table.Td>{cohort.isActive ? t('learning:adminActive') : t('learning:adminInactive')}</Table.Td><Table.Td>{cohort.memberCount}</Table.Td>
      </Table.Tr>)}</Table.Tbody>
    </Table>

    <Card withBorder>
      <Stack>
        <Select label={t('learning:adminSelectedCohort')} data={cohortOptions} value={selectedCohortId} onChange={setSelectedCohortId} />
        <Group align="end">
          <TextInput label={t('learning:adminSearchUsers')} value={search} onChange={(event) => setSearch(event.currentTarget.value)} />
          <Button variant="light" loading={pending} onClick={findUsers}>{t('learning:adminSearch')}</Button>
        </Group>
        <MultiSelect
          searchable
          label={t('learning:adminAssignMembers')}
          data={candidateOptions}
          value={selectedUserIds}
          onChange={setSelectedUserIds}
          nothingFoundMessage={t('learning:adminNoUsers')}
        />
        <Button loading={pending} disabled={!selectedCohortId || selectedUserIds.length === 0} onClick={assign}>{t('learning:adminAssignSelected')}</Button>
      </Stack>
    </Card>

    <Card withBorder>
      <Stack>
        <Text fw={600}>{t('learning:adminCurrentMembers')}</Text>
        <Table>
          <Table.Tbody>{members?.map(member => <Table.Tr key={member.id}>
            <Table.Td>{member.userName}</Table.Td>
            <Table.Td><Button size="xs" color="red" variant="light" loading={pending} onClick={() => member.id && clear(member.id)}>{t('learning:adminRemoveCohort')}</Button></Table.Td>
          </Table.Tr>)}</Table.Tbody>
        </Table>
      </Stack>
    </Card>
  </Stack></WithNavBar></WithRole>
}

export default AdminCohorts

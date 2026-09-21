import { Alert, Button, Card, Code, Group, NumberInput, Stack, Text, TextInput, Title } from '@mantine/core'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import api, { AdminDashboardResponse, Role } from '@Api'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { showErrorMsg } from '@Utils/Shared'

const DashboardCard = ({ dashboard, refresh }: { dashboard: AdminDashboardResponse; refresh: () => Promise<unknown> }) => {
  const { t } = useTranslation()
  const id = dashboard.id ?? ''
  const { data: tokens, mutate: mutateTokens } = api.adminDashboards.useAdminDashboardsListTokens(id, undefined, Boolean(id))
  const [rawLink, setRawLink] = useState<string>()
  const [pending, setPending] = useState(false)

  const run = async (action: () => Promise<void>) => {
    setPending(true)
    try { await action() } catch (error) { showErrorMsg(error, t) } finally { setPending(false) }
  }

  const showToken = (rawToken?: string) => {
    if (rawToken) setRawLink(`${window.location.origin}/dashboard/${id}?token=${rawToken}`)
  }

  const createToken = () => run(async () => {
    const result = await api.adminDashboards.adminDashboardsCreateToken(id, {})
    showToken(result.data.rawToken)
    await Promise.all([mutateTokens(), refresh()])
  })

  const rotateToken = (tokenId: string) => run(async () => {
    const result = await api.adminDashboards.adminDashboardsRotateToken(id, tokenId, {})
    showToken(result.data.rawToken)
    await Promise.all([mutateTokens(), refresh()])
  })

  const revokeToken = (tokenId: string) => run(async () => {
    await api.adminDashboards.adminDashboardsRevokeToken(id, tokenId)
    await Promise.all([mutateTokens(), refresh()])
  })

  return <Card withBorder>
    <Stack>
      <Group justify="space-between">
        <Stack gap={0}>
          <Text fw={600}>{dashboard.name}</Text>
          <Text size="sm" c="dimmed">Top {dashboard.topCount} · {dashboard.activeTokenCount} {t('learning:adminActiveLinks')}</Text>
        </Stack>
        <Button loading={pending} onClick={createToken}>{t('learning:adminCreateReadonlyLink')}</Button>
      </Group>
      {rawLink && <Alert title={t('learning:adminNewReadonlyLink')}><Code style={{ userSelect: 'all' }}>{rawLink}</Code></Alert>}
      {tokens?.map(token => token.tokenId && <Group key={token.tokenId} justify="space-between">
        <Code>{token.tokenId}</Code>
        <Group gap="xs">
          <Button size="xs" variant="light" loading={pending} onClick={() => rotateToken(token.tokenId!)}>{t('learning:adminRotateLink')}</Button>
          <Button size="xs" color="red" variant="light" loading={pending} onClick={() => revokeToken(token.tokenId!)}>{t('learning:adminRevokeLink')}</Button>
        </Group>
      </Group>)}
    </Stack>
  </Card>
}

const AdminDashboards = () => {
  const { data: dashboards, mutate } = api.adminDashboards.useAdminDashboardsList()
  const { t } = useTranslation()
  const [name, setName] = useState('')
  const [topCount, setTopCount] = useState<number | string>(10)
  const [pending, setPending] = useState(false)

  const create = async () => {
    const normalizedTopCount = Number(topCount)
    if (!name.trim() || ![10, 20].includes(normalizedTopCount)) return
    setPending(true)
    try {
      await api.adminDashboards.adminDashboardsCreate({ name: name.trim(), topCount: normalizedTopCount })
      setName('')
      await mutate()
    } catch (error) { showErrorMsg(error, t) } finally { setPending(false) }
  }

  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack>
    <Title order={1}>{t('learning:adminDashboards')}</Title>
    <Card withBorder><Group align="end">
      <TextInput label={t('learning:adminDashboardName')} value={name} onChange={(event) => setName(event.currentTarget.value)} placeholder={t('learning:dashboardTitle')} />
      <NumberInput label={t('learning:adminTopCount')} value={topCount} onChange={setTopCount} min={10} max={20} step={10} />
      <Button loading={pending} onClick={create}>{t('learning:adminCreateDashboard')}</Button>
    </Group></Card>
    {dashboards?.map(dashboard => dashboard.id && <DashboardCard key={dashboard.id} dashboard={dashboard} refresh={mutate} />)}
  </Stack></WithNavBar></WithRole>
}

export default AdminDashboards

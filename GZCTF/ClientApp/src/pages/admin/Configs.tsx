import { FC } from 'react'
import { Divider, SimpleGrid, Stack, Switch, Title } from '@mantine/core'
import AdminPage from '@Components/admin/AdminPage'
import api from '@Api'

const Configs: FC = () => {
  const { data: configs } = api.admin.useAdminGetConfigs({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  return (
    <AdminPage isLoading={!configs}>
      <Stack style={{ width: '80%', minWidth: '70vw' }}>
        <Title order={2}>账户策略</Title>
        <Divider />
        <SimpleGrid cols={2}>
          <Switch/>
        </SimpleGrid>
      </Stack>
    </AdminPage>
  )
}

export default Configs

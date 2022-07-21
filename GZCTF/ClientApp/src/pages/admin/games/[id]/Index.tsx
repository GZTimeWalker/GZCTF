import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Stack, Text } from '@mantine/core'
import LogoHeader from '../../../../components/LogoHeader'
import AdminPage from '../../../../components/admin/AdminPage'

const GameEdit: FC = () => {
  const { id } = useParams()

  return (
    <AdminPage>
      <Stack>
        <LogoHeader />
        <Text>Submission of No.{id}</Text>
      </Stack>
    </AdminPage>
  )
}

export default GameEdit

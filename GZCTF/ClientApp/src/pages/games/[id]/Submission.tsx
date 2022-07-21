import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Stack, Text } from '@mantine/core'
import { Role } from '../../../Api'
import LogoHeader from '../../../components/LogoHeader'
import WithNavBar from '../../../components/WithNavbar'
import WithRole from '../../../components/WithRole'

const Submission: FC = () => {
  const { id } = useParams()

  return (
    <WithNavBar>
      <WithRole requiredRole={Role.Monitor}>
        <Stack>
          <LogoHeader />
          <Text>Submissions of No.{id}</Text>
        </Stack>
      </WithRole>
    </WithNavBar>
  )
}

export default Submission

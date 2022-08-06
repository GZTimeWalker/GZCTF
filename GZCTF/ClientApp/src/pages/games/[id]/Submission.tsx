import { Role } from '@Api/Api'
import LogoHeader from '@Components/LogoHeader'
import WithNavBar from '@Components/WithNavbar'
import WithRole from '@Components/WithRole'
import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Stack, Text } from '@mantine/core'

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

import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Stack, Text } from '@mantine/core'
import LogoHeader from '../../../components/LogoHeader'
import WithNavBar from '../../../components/WithNavbar'
import TeamRank from '../../../components/TeamRank'

const Challenges: FC = () => {
  const { id } = useParams()

  return (
    <WithNavBar>
      <Stack>
        <LogoHeader />
        <Text>Challenges of No.{id}</Text>
        <TeamRank/>
        {/* <ChallengeBoard/> */}
        {/* <NoticeBoard/> */}
      </Stack>
    </WithNavBar>
  )
}

export default Challenges

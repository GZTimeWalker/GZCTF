import { FC } from 'react'
import { Stack } from '@mantine/core'
import TeamRank from '../../../components/TeamRank'
import WithNavBar from '../../../components/WithNavbar'

const Challenges: FC = () => {
  return (
    <WithNavBar>
      <Stack>
        <TeamRank />
        {/* <ChallengeBoard/> */}
        {/* <NoticeBoard/> */}
      </Stack>
    </WithNavBar>
  )
}

export default Challenges

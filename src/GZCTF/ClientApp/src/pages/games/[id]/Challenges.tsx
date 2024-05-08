import { Group, Stack } from '@mantine/core'
import { FC } from 'react'
import ChallengePanel from '@Components/ChallengePanel'
import GameNoticePanel from '@Components/GameNoticePanel'
import TeamRank from '@Components/TeamRank'
import WithGameTab from '@Components/WithGameTab'
import WithNavBar from '@Components/WithNavbar'
import WithRole from '@Components/WithRole'
import { Role } from '@Api'

const Challenges: FC = () => {
  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.User}>
        <WithGameTab>
          <Group gap="sm" justify="space-between" align="flex-start" grow wrap="nowrap">
            <ChallengePanel />
            <Stack maw="20rem">
              <TeamRank />
              <GameNoticePanel />
            </Stack>
          </Group>
        </WithGameTab>
      </WithRole>
    </WithNavBar>
  )
}

export default Challenges

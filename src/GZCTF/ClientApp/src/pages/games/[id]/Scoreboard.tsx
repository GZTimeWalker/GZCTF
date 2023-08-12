import { FC, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Stack } from '@mantine/core'
import MobileScoreboardTable from '@Components/MobileScoreboardTable'
import ScoreboardTable from '@Components/ScoreboardTable'
import TeamRank from '@Components/TeamRank'
import TimeLine from '@Components/TimeLine'
import WithGameTab from '@Components/WithGameTab'
import WithNavBar from '@Components/WithNavbar'
import { useIsMobile } from '@Utils/ThemeOverride'
import api from '@Api'

const Scoreboard: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { data, error } = api.game.useGameChallengesWithTeamInfo(numId, {
    shouldRetryOnError: false,
  })

  const [organization, setOrganization] = useState<string | null>('all')
  const isMobile = useIsMobile(1080)
  const isVertical = useIsMobile()

  return (
    <WithNavBar width="90%" minWidth={0}>
      {isMobile ? (
        <Stack pb="md">
          {data && !error && <TeamRank />}
          {isVertical ? (
            <MobileScoreboardTable
              organization={organization ?? 'all'}
              setOrganization={setOrganization}
            />
          ) : (
            <ScoreboardTable
              organization={organization ?? 'all'}
              setOrganization={setOrganization}
            />
          )}
        </Stack>
      ) : (
        <WithGameTab>
          <Stack pb="2rem">
            <TimeLine organization={organization ?? 'all'} />
            <ScoreboardTable
              organization={organization ?? 'all'}
              setOrganization={setOrganization}
            />
          </Stack>
        </WithGameTab>
      )}
    </WithNavBar>
  )
}

export default Scoreboard

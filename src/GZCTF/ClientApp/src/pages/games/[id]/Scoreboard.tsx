import { Stack } from '@mantine/core'
import { FC, useState } from 'react'
import { useParams } from 'react-router-dom'
import MobileScoreboardTable from '@Components/MobileScoreboardTable'
import ScoreboardTable from '@Components/ScoreboardTable'
import TeamRank from '@Components/TeamRank'
import TimeLine from '@Components/TimeLine'
import WithGameTab from '@Components/WithGameTab'
import WithNavBar from '@Components/WithNavbar'
import { useIsMobile } from '@Utils/ThemeOverride'
import { useGameTeamInfo } from '@Utils/useGame'

const Scoreboard: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { teamInfo, error } = useGameTeamInfo(numId)

  const [organization, setOrganization] = useState<string | null>('all')
  const isMobile = useIsMobile(1080)
  const isVertical = useIsMobile()

  return (
    <WithNavBar width="90%" minWidth={0}>
      {isMobile ? (
        <Stack pt="md">
          {teamInfo && !error && <TeamRank />}
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

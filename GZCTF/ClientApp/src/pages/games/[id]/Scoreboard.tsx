import { FC, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Stack } from '@mantine/core'
import ScoreboardTable from '@Components/ScoreboardTable'
import TimeLine from '@Components/TimeLine'
import WithGameTab from '@Components/WithGameTab'
import WithNavBar from '@Components/WithNavbar'
import api from '@Api'

const Scoreboard: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const [organization, setOrganization] = useState<string | null>('all')
  const { data: scoreboard } = api.game.useGameScoreboard(numId, {
    refreshInterval: 0,
  })

  const currentTimeLine =
    scoreboard?.timeLines && (scoreboard?.timeLines[organization ?? 'all'] ?? [])

  return (
    <WithNavBar width="90%" isLoading={!scoreboard}>
      <WithGameTab>
        <Stack>
          <TimeLine timeLine={currentTimeLine} />
          <ScoreboardTable
            scoreboard={scoreboard!}
            organization={organization ?? 'all'}
            setOrganization={setOrganization}
          />
        </Stack>
      </WithGameTab>
    </WithNavBar>
  )
}

export default Scoreboard

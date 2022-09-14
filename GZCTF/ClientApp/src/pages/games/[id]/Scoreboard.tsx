import { FC } from 'react'
import { Stack } from '@mantine/core'
import ScoreboardTable from '@Components/ScoreboardTable'
import TimeLine from '@Components/TimeLine'
import WithGameTab from '@Components/WithGameTab'
import WithNavBar from '@Components/WithNavbar'

const Scoreboard: FC = () => {
  return (
    <WithNavBar width="90%">
      <WithGameTab>
        <Stack>
          <TimeLine />
          <ScoreboardTable />
        </Stack>
      </WithGameTab>
    </WithNavBar>
  )
}

export default Scoreboard

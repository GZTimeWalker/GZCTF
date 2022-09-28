import { FC, useState } from 'react'
import { Stack } from '@mantine/core'
import ScoreboardTable from '@Components/ScoreboardTable'
import TimeLine from '@Components/TimeLine'
import WithGameTab from '@Components/WithGameTab'
import WithNavBar from '@Components/WithNavbar'

const Scoreboard: FC = () => {
  const [organization, setOrganization] = useState<string | null>('all')

  return (
    <WithNavBar width="90%">
      <WithGameTab>
        <Stack>
          <TimeLine organization={organization ?? 'all'} />
          <ScoreboardTable organization={organization ?? 'all'} setOrganization={setOrganization} />
        </Stack>
      </WithGameTab>
    </WithNavBar>
  )
}

export default Scoreboard

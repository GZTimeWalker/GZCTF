import { FC } from 'react'
import { Stack } from '@mantine/core'
import LogoHeader from '../components/LogoHeader'
import WithNavBar from '../components/WithNavbar'

const Games: FC = () => {
  return (
    <WithNavBar>
      <Stack>
        <LogoHeader />
      </Stack>
    </WithNavBar>
  )
}

export default Games

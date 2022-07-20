import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Stack, Text } from '@mantine/core'
import LogoHeader from '../../../components/LogoHeader'
import WithNavBar from '../../../components/WithNavbar'

const GameDetail: FC = () => {
  const { id } = useParams()

  return (
    <WithNavBar>
      <Stack>
        <LogoHeader />
        <Text>Game No.{id}</Text>
      </Stack>
    </WithNavBar>
  )
}

export default GameDetail

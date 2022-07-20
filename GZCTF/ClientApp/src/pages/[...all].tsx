import { FC } from 'react'
import { Center } from '@mantine/core'
import WithNavBar from '../components/WithNavbar'
import Icon404 from '../components/icon/404Icon'

const Error404: FC = () => {
  return (
    <WithNavBar>
      <Center style={{ height: 'calc(100vh - 32px)' }}>
        <Icon404 />
      </Center>
    </WithNavBar>
  )
}

export default Error404

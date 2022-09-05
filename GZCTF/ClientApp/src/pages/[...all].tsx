import { FC, useEffect } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Stack, Title, Text } from '@mantine/core'
import WithNavBar from '@Components/WithNavbar'
import Icon404 from '@Components/icon/404Icon'
import { usePageTitle } from '@Utils/usePageTitle'

const Error404: FC = () => {
  const navigate = useNavigate()
  const location = useLocation()

  usePageTitle('The Nowhere')

  useEffect(() => {
    if (location.pathname !== '/404') {
      navigate('/404')
    }
  }, [location])

  return (
    <WithNavBar>
      <Stack  spacing={0} align="center" justify="center" style={{ height: 'calc(100vh - 32px)' }}>
          <Icon404 />
          <Title order={2}>这是一处荒芜的地方</Title>
          <Text>你为何会到这里来呢</Text>
      </Stack>
    </WithNavBar>
  )
}

export default Error404

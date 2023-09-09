import { FC, useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Stack, Text, Title } from '@mantine/core'
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
    <WithNavBar minWidth={0}>
      <Stack spacing={0} align="center" justify="center" h="calc(100vh - 32px)">
        <Icon404 />
        <Title order={1} color="#00bfa5" fw="lighter">
          页面不存在
        </Title>
        <Text fw="bold">一处荒芜，为何于此驻足</Text>
      </Stack>
    </WithNavBar>
  )
}

export default Error404

import { FC, useEffect } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Center } from '@mantine/core'
import WithNavBar from '@Components/WithNavbar'
import Icon404 from '@Components/icon/404Icon'
import { usePageTitle } from '@Utils/PageTitle'

const Error404: FC = () => {
  const navigate = useNavigate()
  const location = useLocation()

  usePageTitle('the Nowhere')

  useEffect(() => {
    if (location.pathname !== '/404') {
      navigate('/404')
    }
  }, [location])

  return (
    <WithNavBar>
      <Center style={{ height: 'calc(100vh - 32px)' }}>
        <Icon404 />
      </Center>
    </WithNavBar>
  )
}

export default Error404

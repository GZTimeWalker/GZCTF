import { FC, useEffect } from 'react'
import { Center } from '@mantine/core'
import WithNavBar from '../components/WithNavbar'
import Icon404 from '../components/icon/404Icon'
import { useNavigate, useLocation } from 'react-router-dom'

const Error404: FC = () => {
  const navigate = useNavigate()
  const location = useLocation()

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

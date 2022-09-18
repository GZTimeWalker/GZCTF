import { FC, useEffect, useRef } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Text } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import AccountView from '@Components/AccountView'
import { usePageTitle } from '@Utils/usePageTitle'
import api from '@Api'

const Confirm: FC = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const sp = new URLSearchParams(location.search)
  const token = sp.get('token')
  const email = sp.get('email')
  const runOnce = useRef(false)

  usePageTitle('邮箱验证')

  useEffect(() => {
    if (token && email && !runOnce.current) {
      runOnce.current = true
      api.account
        .accountMailChangeConfirm({ token, email })
        .then(() => {
          showNotification({
            color: 'teal',
            title: '邮箱已验证',
            message: window.atob(email),
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
        })
        .catch(() => {
          showNotification({
            color: 'red',
            title: '邮箱验证失败',
            message: '参数错误，请检查',
            icon: <Icon path={mdiClose} size={1} />,
            disallowClose: true,
          })
        })
        .finally(() => {
          navigate('/')
        })
    }
  }, [])

  return (
    <AccountView>
      <Text>验证邮箱中……</Text>
    </AccountView>
  )
}

export default Confirm

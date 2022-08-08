import { FC, useEffect } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Text } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import AccountView from '@Components/AccountView'
import api from '@Api'

const Verify: FC = () => {
  const location = useLocation()
  const sp = new URLSearchParams(location.search)
  const token = sp.get('token')
  const email = sp.get('email')
  const navigate = useNavigate()

  useEffect(() => {
    if (token && email) {
      api.account
        .accountVerify({ token, email })
        .then(() => {
          showNotification({
            color: 'teal',
            title: '账户已验证，请登录',
            message: window.atob(email),
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          navigate('/account/login')
        })
        .catch(() => {
          showNotification({
            color: 'red',
            title: '账户验证失败',
            message: '参数错误，请检查',
            icon: <Icon path={mdiClose} size={1} />,
            disallowClose: true,
          })
        })
    } else {
      showNotification({
        color: 'red',
        title: '账户验证失败',
        message: '参数错误，请检查',
        icon: <Icon path={mdiClose} size={1} />,
        disallowClose: true,
      })
    }
  })

  return (
    <AccountView>
      <Text>验证中……</Text>
    </AccountView>
  )
}

export default Verify

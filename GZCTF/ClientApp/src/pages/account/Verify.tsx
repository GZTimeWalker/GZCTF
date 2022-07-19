import { FC, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Text } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api from '../../Api'
import AccountView from '../../components/AccountView'

const Verify: FC = () => {
  const params = useParams()
  const token = params.token
  const email = params.email
  const navigate = useNavigate()

  useEffect(() => {
    if (token && email && typeof token === 'string' && typeof email === 'string') {
      api.account
        .accountVerify({ token, email })
        .then(() => {
          showNotification({
            color: 'teal',
            title: '账户已验证，请登录',
            message: Buffer.from(email, 'base64').toString('binary'),
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

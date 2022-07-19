import { FC, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Text } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api from '../../Api'
import AccountView from '../../components/AccountView'

const Confirm: FC = () => {
  const navigate = useNavigate()
  const params = useParams()
  const token = params.token
  const email = params.email

  useEffect(() => {
    if (token && email && typeof token === 'string' && typeof email === 'string') {
      api.account
        .accountMailChangeConfirm({ token, email })
        .then(() => {
          showNotification({
            color: 'teal',
            title: '邮箱已验证',
            message: Buffer.from(email, 'base64'),
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          navigate('/')
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
    } else {
      showNotification({
        color: 'red',
        title: '邮箱验证失败',
        message: '参数错误，请检查',
        icon: <Icon path={mdiClose} size={1} />,
        disallowClose: true,
      })
    }
  })

  return (
    <AccountView>
      <Text>验证邮箱中……</Text>
    </AccountView>
  )
}

export default Confirm

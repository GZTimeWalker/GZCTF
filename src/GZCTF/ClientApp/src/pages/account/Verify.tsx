import { FC, useState } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Button, Text } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import AccountView from '@Components/AccountView'
import { usePageTitle } from '@Utils/usePageTitle'
import api from '@Api'

const Verify: FC = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const sp = new URLSearchParams(location.search)
  const token = sp.get('token')
  const email = sp.get('email')
  const [disabled, setDisabled] = useState(false)

  usePageTitle('账户验证')

  const verify = async (event: React.FormEvent) => {
    event.preventDefault()

    if (!token || !email) {
      showNotification({
        color: 'red',
        title: '账户验证失败',
        message: '参数缺失，请检查',
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setDisabled(true)
    api.account
      .accountVerify({ token, email })
      .then(() => {
        showNotification({
          color: 'teal',
          title: '账户已验证，请登录',
          message: window.atob(email),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        navigate('/account/login')
      })
      .catch(() => {
        showNotification({
          color: 'red',
          title: '账户验证失败',
          message: '参数错误，请检查',
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <AccountView onSubmit={verify}>
      {email && token ? (
        <>
          <Text size="md" fw={500}>
            {window.atob(email)} 你好👋
          </Text>
          <Text size="md" fw={500}>
            请点击下方按钮验证账户
          </Text>
          <Button mt="lg" type="submit" w="50%" disabled={disabled}>
            验证账户
          </Button>
        </>
      ) : (
        <>
          <Text size="md" fw={500}>
            Ouch! 你的链接好像有问题
          </Text>
          <Text size="md" fw={500}>
            请检查链接是否正确后再次访问
          </Text>
        </>
      )}
    </AccountView>
  )
}

export default Verify

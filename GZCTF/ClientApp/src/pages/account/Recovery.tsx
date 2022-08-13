import { FC, useState } from 'react'
import { Link } from 'react-router-dom'
import { TextInput, Button, Anchor } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import AccountView from '@Components/AccountView'
import { usePageTitle } from '@Utils/PageTitle'
import { useReCaptcha } from '@Utils/Recaptcha'
import api from '@Api'

const Recovery: FC = () => {
  const [email, setEmail] = useInputState('')
  const reCaptcha = useReCaptcha('recovery')
  const [disabled, setDisabled] = useState(false)

  usePageTitle('找回账号')

  const onRecovery = async (event: React.FormEvent) => {
    event.preventDefault()

    const token = await reCaptcha?.getToken()

    if (!token) {
      showNotification({
        color: 'orange',
        title: '请等待验证码……',
        message: '请稍后重试',
        loading: true,
        disallowClose: true,
      })
      return
    }

    setDisabled(true)

    showNotification({
      color: 'orange',
      id: 'recovery-status',
      title: '请求已发送……',
      message: '等待服务器验证',
      loading: true,
      autoClose: false,
      disallowClose: true,
    })

    api.account
      .accountRecovery({
        email,
        gToken: token,
      })
      .then(() => {
        updateNotification({
          id: 'recovery-status',
          color: 'teal',
          title: '一封恢复邮件已发送',
          message: '请检查你的邮箱及垃圾邮件~',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
      })
      .catch((err) => {
        updateNotification({
          id: 'recovery-status',
          color: 'red',
          title: '遇到了问题',
          message: `${err.response.data.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <AccountView onSubmit={onRecovery}>
      <TextInput
        required
        label="邮箱"
        placeholder="ctf@example.com"
        type="email"
        style={{ width: '100%' }}
        value={email}
        disabled={disabled}
        onChange={(event) => setEmail(event.currentTarget.value)}
      />
      <Anchor
        sx={(theme) => ({
          fontSize: theme.fontSizes.xs,
          alignSelf: 'end',
        })}
        component={Link}
        to="/account/login"
      >
        准备好登录？
      </Anchor>
      <Button disabled={disabled} fullWidth onClick={onRecovery}>
        发送重置邮件
      </Button>
    </AccountView>
  )
}

export default Recovery

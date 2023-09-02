import { FC, useState } from 'react'
import { Link } from 'react-router-dom'
import { TextInput, Button, Anchor, Box } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import AccountView from '@Components/AccountView'
import { useCaptcha } from '@Utils/useCaptcha'
import { usePageTitle } from '@Utils/usePageTitle'
import api from '@Api'

const Recovery: FC = () => {
  const [email, setEmail] = useInputState('')
  const captcha = useCaptcha('recovery')
  const [disabled, setDisabled] = useState(false)

  usePageTitle('找回账号')

  const onRecovery = async (event: React.FormEvent) => {
    event.preventDefault()

    const token = await captcha?.getChallenge()

    if (!token) {
      showNotification({
        color: 'orange',
        title: '请等待验证码……',
        message: '请稍后重试',
        loading: true,
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
    })

    api.account
      .accountRecovery({
        email,
        challenge: token,
      })
      .then(() => {
        updateNotification({
          id: 'recovery-status',
          color: 'teal',
          title: '一封恢复邮件已发送',
          message: '请检查你的邮箱及垃圾邮件~',
          icon: <Icon path={mdiCheck} size={1} />,
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
        w="100%"
        value={email}
        disabled={disabled}
        onChange={(event) => setEmail(event.currentTarget.value)}
      />
      <Box id="captcha" />
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

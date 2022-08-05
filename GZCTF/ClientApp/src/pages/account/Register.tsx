import { FC, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Button, Anchor, TextInput, PasswordInput } from '@mantine/core'
import { getHotkeyHandler, useInputState } from '@mantine/hooks'
import { showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api from '../../Api'
import AccountView from '../../components/AccountView'
import StrengthPasswordInput from '../../components/StrengthPasswordInput'
import { useReCaptcha } from '../../utils/Recaptcha'

const Register: FC = () => {
  const [pwd, setPwd] = useInputState('')
  const [retypedPwd, setRetypedPwd] = useInputState('')
  const [uname, setUname] = useInputState('')
  const [email, setEmail] = useInputState('')
  const [disabled, setDisabled] = useState(false)

  const navigate = useNavigate()
  const reCaptcha = useReCaptcha('register')

  const onRegister = async () => {
    if (pwd !== retypedPwd) {
      showNotification({
        color: 'red',
        title: '请检查输入',
        message: '重复密码有误',
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

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
      id: 'register-status',
      title: '请求已发送……',
      message: '等待服务器验证',
      loading: true,
      autoClose: false,
      disallowClose: true,
    })

    api.account
      .accountRegister({
        userName: uname,
        password: pwd,
        email,
        gToken: token,
      })
      .then(() => {
        updateNotification({
          id: 'register-status',
          color: 'teal',
          title: '一封注册邮件已发送',
          message: '请检查你的邮箱及垃圾邮件~',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
        navigate('/account/login')
      })
      .catch((err) => {
        updateNotification({
          id: 'register-status',
          color: 'red',
          title: '遇到了问题',
          message: `${err.response.data.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        })
        setDisabled(false)
      })
  }

  const enterHandler = getHotkeyHandler([['Enter', onRegister]])

  return (
    <AccountView>
      <TextInput
        required
        label="邮箱"
        type="email"
        placeholder="ctf@example.com"
        style={{ width: '100%' }}
        value={email}
        disabled={disabled}
        onChange={(event) => setEmail(event.currentTarget.value)}
        onKeyDown={enterHandler}
      />
      <TextInput
        required
        label="用户名"
        type="text"
        placeholder="ctfer"
        style={{ width: '100%' }}
        value={uname}
        disabled={disabled}
        onChange={(event) => setUname(event.currentTarget.value)}
        onKeyDown={enterHandler}
      />
      <StrengthPasswordInput
        value={pwd}
        onChange={(event) => setPwd(event.currentTarget.value)}
        disabled={disabled}
        onKeyDown={enterHandler}
      />
      <PasswordInput
        required
        value={retypedPwd}
        onChange={(event) => setRetypedPwd(event.currentTarget.value)}
        disabled={disabled}
        label="重复密码"
        style={{ width: '100%' }}
        error={pwd !== retypedPwd}
        onKeyDown={enterHandler}
      />
      <Anchor
        sx={(theme) => ({
          fontSize: theme.fontSizes.xs,
          alignSelf: 'end',
        })}
        component={Link}
        to="/account/login"
      >
        已经拥有账户？
      </Anchor>
      <Button fullWidth onClick={onRegister} disabled={disabled}>
        注册
      </Button>
    </AccountView>
  )
}

export default Register
